﻿// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (C) 2007-2010 LShift Ltd., Cohesive Financial
//   Technologies LLC., and Rabbit Technologies Ltd.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//   The contents of this file are subject to the Mozilla Public License
//   Version 1.1 (the "License"); you may not use this file except in
//   compliance with the License. You may obtain a copy of the License at
//   http://www.rabbitmq.com/mpl.html
//
//   Software distributed under the License is distributed on an "AS IS"
//   basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//   License for the specific language governing rights and limitations
//   under the License.
//
//   The Original Code is The RabbitMQ .NET Client.
//
//   The Initial Developers of the Original Code are LShift Ltd,
//   Cohesive Financial Technologies LLC, and Rabbit Technologies Ltd.
//
//   Portions created before 22-Nov-2008 00:00:00 GMT by LShift Ltd,
//   Cohesive Financial Technologies LLC, or Rabbit Technologies Ltd
//   are Copyright (C) 2007-2008 LShift Ltd, Cohesive Financial
//   Technologies LLC, and Rabbit Technologies Ltd.
//
//   Portions created by LShift Ltd are Copyright (C) 2007-2010 LShift
//   Ltd. Portions created by Cohesive Financial Technologies LLC are
//   Copyright (C) 2007-2010 Cohesive Financial Technologies
//   LLC. Portions created by Rabbit Technologies Ltd are Copyright
//   (C) 2007-2010 Rabbit Technologies Ltd.
//
//   All Rights Reserved.
//
//   Contributor(s): ______________________________________.
//
//---------------------------------------------------------------------------
using NUnit.Framework;

using System;
using System.IO;
using System.Text;
using System.Collections;

using RabbitMQ.Client.Impl;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Events;
using RabbitMQ.Util;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestRecoverAfterCancel
    {
        IConnection Connection;
        IModel Channel;
        String Queue;
        delegate void Recovery(IModel ch);

        public int ModelNumber(IModel model)
        {
            return ((ModelBase)model).m_session.ChannelNumber;
        }

        [SetUp] public void Connect()
        {
            Connection = new ConnectionFactory().CreateConnection();
            Channel = Connection.CreateModel();
            Queue = Channel.QueueDeclare();
        }

        [TearDown] public void Disconnect()
        {
            Channel.Close();
            Connection.Close();
        }

        [Test]
        public void TestRecoverAfterCancel_()
        {
            RecoveryRunner(new Recovery(delegate (IModel ch)
            {
                Channel.BasicRecover(false);
            }));
        }

        [Test]
        public void TestRecoverAsyncAfterCancel()
        {
            RecoveryRunner(new Recovery(delegate (IModel ch)
            {
                Channel.BasicRecoverAsync(false);
                Thread.Sleep(500);
            }));
        }

        private void RecoveryRunner(Recovery recover)
        {
            UTF8Encoding enc = new UTF8Encoding();
            Channel.BasicPublish("", Queue, null, enc.GetBytes("message"));
            QueueingBasicConsumer Consumer = new QueueingBasicConsumer(Channel);
            QueueingBasicConsumer DefaultConsumer = new QueueingBasicConsumer(Channel);
            Channel.DefaultConsumer = DefaultConsumer;
            String CTag = Channel.BasicConsume(Queue, null, Consumer);
            BasicDeliverEventArgs Event = (BasicDeliverEventArgs)Consumer.Queue.Dequeue();
            Channel.BasicCancel(CTag);
            recover(Channel);

            // The server will now redeliver us the first message again, with the
            // same ctag, but we're not set up to handle it with a standard
            // consumer - it should end up with the default one.

            BasicDeliverEventArgs Event2 = (BasicDeliverEventArgs)DefaultConsumer.Queue.Dequeue();

            Assert.AreEqual(Event.Body, Event2.Body);
            Assert.IsFalse(Event.Redelivered);
            Assert.IsTrue(Event2.Redelivered);
        }
    }
}
