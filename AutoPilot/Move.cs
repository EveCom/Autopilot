using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;

namespace EveComFramework
{
    class Move : State
    {

        public Move()
        {
            DefaultFrequency = 1000;
        }

        public bool Busy { get { return !Idle; } }

        public void DockAt(Func<IDockable> Dockable)
        {
            QueueState(DockAtState, -1, Dockable);
        }

        public void DockAt(IDockable Dockable)
        {
            DockAt(() => Dockable);
        }

        bool DockAtState(object[] Params)
        {
            if (Session.InStation)
            {
                return true;
            }
            Params = Params ?? new object[] { };
            IDockable Target;
            if (Params.Length == 0)
            {
                return true;
            }
            Target = ((Func<IDockable>)Params[0])();
            Target.Dock();
            WaitFor(10, () => Session.InStation, () => MyShip.ToEntity.Mode == EntityMode.Warping);
            QueueState(DockAtState, -1, Params);
            return true;
        }

        public void AutoPilot()
        {
            QueueState(CheckAutoPilot);
        }

        bool CheckAutoPilot(object[] Params)
        {
            if (Route.NextWaypoint == null)
            {
                return true;
            }
            if (Session.InStation)
            {
                if (Route.NextWaypoint.GroupID == Group.Station && Route.NextWaypoint.ID != Session.StationID)
                {
                    Command.CmdExitStation.Execute();
                    WaitFor(30, () => Session.InSpace);
                    QueueState(CheckAutoPilot);
                }
                if (Route.NextWaypoint.GroupID == Group.SolarSystem && Route.NextWaypoint.ID != Session.SolarSystemID)
                {
                    Command.CmdExitStation.Execute();
                    WaitFor(30, () => Session.InSpace);
                    QueueState(CheckAutoPilot);
                }
            }
            else
            {
                QueueState(AutoPilotState);
            }
            return true;
        }

        bool AutoPilotState(object[] Params)
        {
            if (Route.NextWaypoint == null)
            {
                return true;
            }
            if (Route.NextWaypoint.GroupID == Group.Station)
            {
                DockAt(Route.NextWaypoint);
                return true;
            }
            if (Route.NextWaypoint.GroupID == Group.SolarSystem && Route.NextWaypoint.ID != Session.SolarSystemID)
            {
                long nextSystem = Route.NextWaypoint.ID;
                Entity nextSystemGate = Entity.All.FirstOrDefault(ent => ent.GroupID == Group.Stargate && ent.JumpDest == nextSystem);
                if (nextSystemGate == null)
                {
                    return true;
                }
                nextSystemGate.Jump();
                WaitFor(30, () => Session.SolarSystemID == nextSystem, () => MyShip.ToEntity.Mode == EntityMode.Warping);
                QueueState(AutoPilotState);
            }
            return true;
        }


    }
}
