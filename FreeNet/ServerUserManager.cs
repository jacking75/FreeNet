﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNet
{
    /// <summary>
    /// 현재 접속중인 전체 유저를 관리하는 클래스.
    /// </summary>
    public class ServerUserManager
    {
        //TODO: ConcureentDictionary를 사용한다. 그런데 foreach에서 스레드 세이프한지 테스트 하자. 스택오버플로어에서는 스레드 세이프하다고 한다.
        object cs_user;
        List<UserToken> users;

        Timer timer_heartbeat;
        long heartbeat_duration;


        public ServerUserManager()
        {
            this.cs_user = new object();
            this.users = new List<UserToken>();
        }


        public void start_heartbeat_checking(uint check_interval_sec, uint allow_duration_sec)
        {
            this.heartbeat_duration = allow_duration_sec * 10000000;
            this.timer_heartbeat = new Timer(check_heartbeat, null, 1000 * check_interval_sec, 1000 * check_interval_sec);
        }


        public void add(UserToken user)
        {
            lock (this.cs_user)
            {
                this.users.Add(user);
            }
        }


        public void remove(UserToken user)
        {
            lock (this.cs_user)
            {
                this.users.Remove(user);
            }
        }


        public bool is_exist(UserToken user)
        {
            lock (this.cs_user)
            {
                return this.users.Exists(obj => obj == user);
            }
        }


        public int get_total_count()
        {
            return this.users.Count;
        }


        void check_heartbeat(object state)
        {
            long allowed_time = DateTime.Now.Ticks - this.heartbeat_duration;

            lock (this.cs_user)
            {
                for (int i = 0; i < this.users.Count; ++i)
                {
                    long heartbeat_time = this.users[i].latest_heartbeat_time;
                    if (heartbeat_time >= allowed_time)
                    {
                        continue;
                    }

                    this.users[i].disconnect();
                }
            }
        }


    }
}
