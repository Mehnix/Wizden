using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._BPL.Botany.Items.Components
{
    /// <summary>
    /// A swab that stores other swabs and uses their SeedData
    /// This could be implemented into the base BotanySwabSystem but seperating it reduces interference with upstream code
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class SwabApplicatorComponent : Component
    {
        /// <summary>
        ///     Container of swab.
        /// </summary>
        [DataField, AutoNetworkedField]
        public string? SwabContainer = "swab";
    }
}
