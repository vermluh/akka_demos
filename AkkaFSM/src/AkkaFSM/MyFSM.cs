namespace AkkaFSM
{
    using System;
    using Akka.Actor;

    public class MyFSM : FSM<int, object>
    {
        #region constructor
        public MyFSM(IActorRef target)
        {
            this.Target = target;
            this.StartWith(0, new object());

            When(0, evnt =>
            {
                if (evnt.FsmEvent.Equals("tick")) return this.GoTo(1);
                return null;
            });

            When(1, evnt =>
            {
                if (evnt.FsmEvent.Equals("tick")) return this.GoTo(0);
                return null;
            });

            WhenUnhandled(evnt =>
            {
                if (evnt.FsmEvent.Equals("reply")) return this.Stay().Replying("reply");
                return null;
            });

            this.Initialize();
        }
        #endregion

        #region properties
        public IActorRef Target { get; private set; }
        #endregion

        #region methods
        #region protected override void PreRestart(Exception, object)
        protected override void PreRestart(Exception reason, object message)
        {
            Target.Tell("restarted");
        }
        #endregion
        #endregion
    }
}
