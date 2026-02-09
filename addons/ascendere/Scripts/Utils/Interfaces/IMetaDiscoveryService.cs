#if TOOLS
using System.Collections.Generic;

namespace Ascendere.Utils
{
    /// <summary>
    /// Interface for Meta Framework attribute discovery services.
    /// </summary>
    public interface IMetaDiscoveryService
    {
        /// <summary>
        /// Discovers all Meta Framework attributes in the current application domain.
        /// </summary>
        /// <returns>A discovery result containing all found components, entities, and systems.</returns>
        MetaDiscoveryResult DiscoverMetaTypes();

        /// <summary>
        /// Discovers only components with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered components.</returns>
        List<MetaComponentInfo> DiscoverComponents();

        /// <summary>
        /// Discovers only entities with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered entities.</returns>
        List<MetaEntityInfo> DiscoverEntities();

        /// <summary>
        /// Discovers only systems with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered systems.</returns>
        List<MetaSystemInfo> DiscoverSystems();
    }
}
#endif
