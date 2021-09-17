namespace OlegMC.REST_API.Model
{
    /// <summary>
    /// The basic object used to identify the current plan.
    /// </summary>
    public class PlanModel
    {
        /// <summary>
        /// The Plan Name.
        /// </summary>
        public virtual string Name { get; }

        /// <summary>
        /// The ammount of ram allocated to each plan.
        /// </summary>
        public virtual int RAM { get; }

        public virtual int MaxBackups { get; set; }

        /// <summary>
        /// The user accessing the server console
        /// </summary>
        public virtual string Username { get; private set; }

        public PlanModel(string username)
        {
            Username = username;
        }

        public static PlanModel GetBasedOnName(string plan, string username)
        {
            return plan.ToLower() switch
            {
                "byos" => new BYOSPlan(username),
                "basic" => new BasicPlan(username),
                "intermidate" => new IntermidatePlan(username),
                "advanced" => new AdvancedPlan(username),
                "pro" => new PROPlan(username),
                "elite" => new ElitePlan(username),
                _ => throw new System.Exception("No Plan was found"),
            };
        }
    }

    /// <summary>
    /// A basic plan
    /// </summary>
    public class BYOSPlan : PlanModel
    {
        public override string Name => "BYOS";
        public override int RAM => 4;
        public override int MaxBackups { get; set; }

        public BYOSPlan(string username) : base(username)
        {
        }
    }

    /// <summary>
    /// A basic plan
    /// </summary>
    public class BasicPlan : PlanModel
    {
        public override string Name => "Basic";
        public override int RAM => 4;
        public override int MaxBackups => 5;

        public BasicPlan(string username) : base(username)
        {
        }
    }

    /// <summary>
    /// The Intermidate Plan
    /// </summary>
    public class IntermidatePlan : PlanModel
    {
        public override string Name => "Intermidate";
        public override int RAM => 6;
        public override int MaxBackups => 10;

        public IntermidatePlan(string username) : base(username)
        {
        }
    }

    /// <summary>
    /// The Advanced Plan
    /// </summary>
    public class AdvancedPlan : PlanModel
    {
        public override string Name => "Advanced";
        public override int RAM => 8;
        public override int MaxBackups => 15;

        public AdvancedPlan(string username) : base(username)
        {
        }
    }

    /// <summary>
    /// The PRO Plan
    /// </summary>
    public class PROPlan : PlanModel
    {
        public override string Name => "PRO";
        public override int RAM => 10;
        public override int MaxBackups => 20;

        public PROPlan(string username) : base(username)
        {
        }
    }

    /// <summary>
    /// The Elite Plan
    /// </summary>
    public class ElitePlan : PlanModel
    {
        public override string Name => "Elite";
        public override int RAM => 16;
        public override int MaxBackups => 25;

        public ElitePlan(string username) : base(username)
        {
        }
    }
}