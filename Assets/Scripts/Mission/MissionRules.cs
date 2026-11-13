namespace Mission
{
    public static class MissionRules
    {
        // The mission is complete when the camp accepted everything that was ordered
        // and did not have to reject anything. It only reads the state, it changes nothing.
        public static bool IsMissionComplete(Ledger ledger, Camp camp)
        {
            if (camp.Rejected.Count > 0)
                return false;

            foreach (var entry in ledger.Order)
            {
                if (camp.CountAccepted(entry.Key) < entry.Value)
                    return false;
            }

            return true;
        }
    }
}
