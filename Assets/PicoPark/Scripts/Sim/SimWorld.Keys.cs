namespace PicoPark
{
    public partial class SimWorld
    {
        public void ClearRegisteredKeys()
        {
            keyActorIds.Clear();
        }

        public void RegisterKey(int actorId)
        {
            keyActorIds.Add(actorId);
        }

        public int KeyCount()
        {
            return keyActorIds.Count;
        }

        public int DeliveredKeyCount()
        {
            int n = 0;
            for (int i = 0; i < keyActorIds.Count; i++)
            {
                SimActor a = GetActor(keyActorIds[i]);
                if (a != null && a.delivered) n++;
            }
            return n;
        }

        public bool AllPlayersInDoor()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null) continue;
                if (!p.inDoor) return false;
            }
            return PlayerSimulationCount > 0;
        }

        public void ApplyKeyControlButton(SimButton b)
        {
            for (int i = 0; i < keyActorIds.Count; i++)
            {
                SimActor key = GetActor(keyActorIds[i]);
                if (key != null)
                {
                    key.active = true;
                    key.pos = key.spawnPos;
                }
            }

            PPKeyMarker.RevealAllControlledKeys();
        }
    }
}
