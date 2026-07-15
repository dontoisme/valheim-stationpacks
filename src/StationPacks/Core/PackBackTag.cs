using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Marks the back-mesh container so we can find it after the game has instantiated it onto a
    /// character - more robust than matching by name, and it survives being reparented onto a bone.
    /// </summary>
    internal sealed class PackBackTag : MonoBehaviour
    {
        /// <summary>True once the container has been reparented onto the spine bone.</summary>
        public bool Bound;

        /// <summary>
        /// The attach_skin instance this mesh was born under. We reparent onto the spine bone so the
        /// pack follows the torso, but that also moves it OUT of the hierarchy the game destroys on
        /// unequip - so it would otherwise be orphaned on the skeleton and pile up on every re-equip.
        /// We watch this reference and self-destruct once the game tears the source down.
        /// </summary>
        public Transform Source;

        // The pack's intended local transform RELATIVE TO THE SPINE BONE. The bone binder applies
        // these after reparenting, so what the tune command edits and what we bake into PackDefinition
        // are the same space - no attach_skin-vs-Spine2 mismatch.
        public float TargetScale = 0.16f;
        public Vector3 TargetOffset = Vector3.zero;
        public Vector3 TargetEuler = Vector3.zero;

        private void LateUpdate()
        {
            // Unity-null once the game destroys the shoulder item's attach_skin (i.e. on unequip or a
            // switch to another shoulder item). That's our cue to clean ourselves up.
            if (Bound && Source == null)
                Destroy(gameObject);
        }
    }
}
