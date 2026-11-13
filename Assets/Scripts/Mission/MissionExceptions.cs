using System;

namespace Mission
{
    public class WagonOverloadedException : Exception
    {
        public WagonOverloadedException(string message) : base(message)
        {
        }
    }

    public class CargoExplodedException : Exception
    {
        public CargoExplodedException(string message) : base(message)
        {
        }
    }

    public class CargoAlreadyLoadedException : Exception
    {
        public CargoAlreadyLoadedException(string message) : base(message)
        {
        }
    }

    public class HoundRefusedException : Exception
    {
        public HoundRefusedException(string message) : base(message)
        {
        }
    }
}
