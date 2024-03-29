﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;

namespace EveComFramework
{
    class State
    {
        internal class StateQueue
        {
            internal Func<object[], bool> State { get; set; }
            internal object[] Params { get; set; }
            internal int Frequency { get; set; }
            internal StateQueue(Func<object[], bool> State, int Frequency, object[] Params)
            {
                this.State = State;
                this.Params = Params;
                this.Frequency = Frequency;
            }
            public override string ToString()
            {
                return State.Method.Name;
            }
        }

        public int DefaultFrequency { get; set; }
        public DateTime NextPulse { get; set; }
        internal LinkedList<StateQueue> States = new LinkedList<StateQueue>();
        internal StateQueue CurState;
        public bool Idle { get { return CurState == null; } }

        public State()
        {
            DefaultFrequency = 1000;
            EVEFrame.OnFrame += OnFrame;
        }

        ~State()
        {
            EVEFrame.OnFrame -= OnFrame;
        }

        public void QueueState(Func<object[], bool> State, int Frequency = -1, params object[] Params)
        {
            States.AddFirst(new StateQueue(State, ((Frequency == -1) ? DefaultFrequency : Frequency), Params));
            if (CurState == null)
            {
                CurState = States.Last();
                States.RemoveLast();
            }
        }

        public void InsertState(Func<object[], bool> State, int Frequency = -1, params object[] Params)
        {
            States.AddLast(new StateQueue(State, ((Frequency == -1) ? DefaultFrequency : Frequency), Params));
            if (CurState == null)
            {
                CurState = States.Last();
                States.RemoveLast();
            }
        }

        public void DislodgeCurState(Func<object[], bool> State, int Frequency = -1, params object[] Params)
        {
            if (CurState != null)
            {
                States.AddLast(CurState);
            }
            CurState = new StateQueue(State, ((Frequency == -1) ? DefaultFrequency : Frequency), Params);
        }

        public void Clear()
        {
            States.Clear();
            CurState = null;
        }

        public void ClearCurState()
        {
            CurState = null;
            if (States.Count > 0)
            {
                CurState = States.Last();
                States.RemoveLast();
            }
        }

        public bool WaitForState(object[] Params)
        {
            switch (Params.Length)
            {
                case 1:
                    if ((int)Params[0] >= 1)
                    {
                        CurState.Params[0] = (int)Params[0] - 1;
                        return false;
                    }
                    break;
                case 2:
                    if ((int)Params[0] >= 1 && !((Func<bool>)Params[1])())
                    {
                        CurState.Params[0] = (int)Params[0] - 1;
                        return false;
                    }
                    break;
                case 4:
                    if ((int)Params[0] >= 1 && !((Func<bool>)Params[1])())
                    {
                        if (((Func<bool>)Params[2])())
                        {
                            CurState.Params[0] = (int)Params[3];
                        }
                        else
                        {
                            CurState.Params[0] = (int)Params[0] - 1;
                        }
                        return false;
                    }
                    break;
            }
            return true;
        }

        public void WaitFor(int TimeOut, Func<bool> Test = null, Func<bool> Reset = null)
        {
            if (Reset != null)
            {
                InsertState(WaitForState, -1, TimeOut, Test, Reset, TimeOut);
            }
            else if (Test != null)
            {
                InsertState(WaitForState, -1, TimeOut, Test);
            }
            else
            {
                InsertState(WaitForState, -1, TimeOut);
            }
        }

        void OnFrame(object sender, EventArgs e)
        {
            Random Delta = new Random();
            if (DateTime.Now > NextPulse.AddMilliseconds(Delta.Next(100)))
            {
                if (CurState != null && Session.Safe && Session.NextSessionChange < Session.Now)
                {
                    if (CurState.State(CurState.Params))
                    {
                        if (States.Count > 0)
                        {
                            CurState = States.Last();
                            States.RemoveLast();
                        }
                        else
                        {
                            CurState = null;
                        }
                    }
                }

                if (CurState == null)
                {
                    NextPulse = DateTime.Now.AddMilliseconds(DefaultFrequency);
                }
                else
                {
                    NextPulse = DateTime.Now.AddMilliseconds(CurState.Frequency);
                }
            }
        }
    }
}
