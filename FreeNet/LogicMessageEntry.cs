﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNet
{
    /// <summary>
    /// 수신된 패킷을 받아 로직 스레드에서 분배하는 역할을 담당한다.
    /// </summary>
    public class LogicMessageEntry : IMessageDispatcher
    {
        NetworkService service;
        ILogicQueue message_queue;
        AutoResetEvent logic_event;


        public LogicMessageEntry(NetworkService service)
        {
            this.service = service;
            this.message_queue = new DoubleBufferingQueue();
            this.logic_event = new AutoResetEvent(false);
        }


        /// <summary>
        /// 로직 스레드 시작.
        /// </summary>
        public void start()
        {
            Thread logic = new Thread(this.do_logic);
            logic.Start();
        }


        void IMessageDispatcher.on_message(UserToken user, ArraySegment<byte> buffer)
        {
            // 여긴 IO스레드에서 호출된다.
            // 완성된 패킷을 메시지큐에 넣어준다.
            Packet msg = new Packet(buffer, user);
            this.message_queue.Enqueue(msg);

            // 로직 스레드를 깨워 일을 시킨다.
            this.logic_event.Set();
        }


        /// <summary>
        /// 로직 스레드.
        /// </summary>
        void do_logic()
        {
            while (true)
            {
                // 패킷이 들어오면 알아서 깨워 주겠지.
                this.logic_event.WaitOne();

                // 메시지를 분배한다.
                dispatch_all(this.message_queue.TakeAll());
            }
        }


        void dispatch_all(Queue<Packet> queue)
        {
            while (queue.Count > 0)
            {
                Packet msg = queue.Dequeue();
                if (!this.service.usermanager.is_exist(msg.Owner))
                {
                    continue;
                }

                msg.Owner.on_message(msg);
            }
        }
    }
}
