// This source code is dual-licensed under the Apache License, version
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
//       http://www.Apache.Org/licenses/LICENSE-2.0
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
//   http://www.Rabbitmq.Com/mpl.Html
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
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{

  [TestFixture]
  public class TestIModelAllocation
  {
    public const int CHANNEL_COUNT = 100;

    IConnection C;

    public int ModelNumber(IModel model)
    {
      return ((ModelBase)model).m_session.ChannelNumber;
    }
  
    [SetUp] public void Connect()
    {
      C = new ConnectionFactory().CreateConnection();
    }

    [TearDown] public void Disconnect()
    {
      C.Close();
    }


    [Test] public void AllocateInOrder()
    {
      for(int i = 1; i <= CHANNEL_COUNT; i++)
        Assert.AreEqual(i, ModelNumber(C.CreateModel()));
    }

    [Test] public void AllocateAfterFreeingLast() {
      IModel ch = C.CreateModel();
      Assert.AreEqual(1, ModelNumber(ch));
      ch.Close();
      ch = C.CreateModel();
      Assert.AreEqual(1, ModelNumber(ch));
    }

    public int CompareModels(IModel x, IModel y)
    {
      int i = ModelNumber(x);
      int j = ModelNumber(y);
      return (i < j) ? -1 : (i == j) ? 0 : 1;
    }

    [Test] public void AllocateAfterFreeingMany() {
      List<IModel> channels = new List<IModel>();

      for(int i = 1; i <= CHANNEL_COUNT; i++)
        channels.Add(C.CreateModel());

      foreach(IModel channel in channels){
        channel.Close();
      }

      channels = new List<IModel>();

      for(int j = 1; j <= CHANNEL_COUNT; j++)
        channels.Add(C.CreateModel());

      // In the current implementation the list should actually
      // already be sorted, but we don't want to force that behaviour
      channels.Sort(CompareModels);

      int k = 1;
      foreach(IModel channel in channels)
        Assert.AreEqual(k++, ModelNumber(channel));
    }
  }
}
