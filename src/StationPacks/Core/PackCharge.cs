using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Charge accounting.
    ///
    /// The one subtlety: a pack must only be charged when it is *what made the action possible*. If
    /// we drained on every placement, the pack would bleed charge while you build inside your own
    /// base next to a real workbench - the single most likely balance bug in the mod.
    ///
    /// So the postfix records a grant (station name + frame). The build patches drain only when a
    /// successful action names the same station on the same frame. Vanilla calls
    /// HaveBuildStationInRange immediately before acting, so a genuine grant is always fresh.
    /// </summary>
    public static class PackCharge
    {
        private static string _grantedStation;
        private static int _grantedFrame = -1;

        public static void NoteGrant(string stationName)
        {
            _grantedStation = stationName;
            _grantedFrame = Time.frameCount;
        }

        private static bool GrantedThisFrame(string stationName) =>
            _grantedFrame == Time.frameCount && _grantedStation == stationName;

        /// <summary>
        /// Spends charge if - and only if - the pack is what enabled this action on this piece.
        /// Returns true if charge was actually spent.
        /// </summary>
        public static bool SpendFor(Player player, Piece piece, float cost)
        {
            if (player == null || piece == null || cost <= 0f) return false;
            if (piece.m_craftingStation == null) return false;

            var stationName = piece.m_craftingStation.m_name;
            if (!GrantedThisFrame(stationName)) return false;   // a real station covered this, not us

            var pack = EquippedPackResolver.TryGetPack(player, stationName);
            if (pack == null) return false;

            var before = pack.m_durability;
            pack.m_durability = Mathf.Max(0f, before - cost);

            if (pack.m_durability <= 0f && before > 0f)
            {
                player.Message(MessageHud.MessageType.Center, "$sp_pack_depleted");
            }
            else if (SPConfig.ShowChargeMessages.Value)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"{Localization.instance.Localize(pack.m_shared.m_name)} " +
                    $"{Mathf.CeilToInt(pack.m_durability)}/{Mathf.CeilToInt(pack.GetMaxDurability())}");
            }

            return true;
        }
    }
}
