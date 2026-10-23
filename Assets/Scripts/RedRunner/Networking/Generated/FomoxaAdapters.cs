namespace Generated
{
    public static class FomoxaAdapters
    {
        public static void RegisterAll(global::Fomoxa.Networking.FomoxaRegistry registry)
        {
            registry.SetSchema(NetSchema.Build());
            registry.SetCodec(MessageBundleNetAdapter.Instance);
            registry.SetCodec(ReliableAckNetAdapter.Instance);
            registry.SetCodec(LocalPeerNetAdapter.Instance);
            registry.SetCodec(ObjectSpawnNetAdapter.Instance);
            registry.SetCodec(ObjectSceneSpawnNetAdapter.Instance);
            registry.SetCodec(ObjectDespawnNetAdapter.Instance);
            registry.SetCodec(ObjectOwnerChangeNetAdapter.Instance);
            registry.SetCodec(StateDeltaNetAdapter.Instance);
            registry.SetCodec(StateResyncNetAdapter.Instance);
            registry.SetCodec(TransformUpdateNetAdapter.Instance);
            registry.SetCodec(TransformSettleNetAdapter.Instance);
            registry.SetCodec(AnimatorStateNetAdapter.Instance);
            registry.SetCodec(SceneLoadNetAdapter.Instance);
            registry.SetCodec(SceneUnloadNetAdapter.Instance);
            registry.SetCodec(SceneLoadedNetAdapter.Instance);
            registry.SetCodec(TickPingNetAdapter.Instance);
            registry.SetCodec(TickPongNetAdapter.Instance);
            registry.SetCodec(InputFramesNetAdapter.Instance);
            registry.SetCodec(ReconcileStateNetAdapter.Instance);
            registry.SetCodec(PeerLeaveNetAdapter.Instance);
            registry.SetCodec(FragmentStartNetAdapter.Instance);
            registry.SetCodec(FragmentPartNetAdapter.Instance);
            registry.SetCodec(SceneFileNetAdapter.Instance);
            registry.Channels.Set(AnimatorStateNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(FragmentPartNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(FragmentStartNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(LocalPeerNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(ObjectDespawnNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(ObjectOwnerChangeNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(ObjectSceneSpawnNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(ObjectSpawnNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(SceneLoadNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(SceneLoadedNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(SceneUnloadNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(StateDeltaNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(StateResyncNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(TransformSettleNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(LevelStateNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
            registry.Channels.Set(PlayerStateNetCodec.MessageId, global::Fomoxa.Networking.Channel.ReliableOrdered);
        }
    }
}
