using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public abstract class ClientBoundRequest
    {
        private string _externalClientApplicationId;
        private bool _hasAdministratorRights;
        private bool _hasGlobalQueryRights;

        public virtual string ExternalClientApplicationId()
        {
            return _externalClientApplicationId;
        }

        public virtual bool HasAdmnistratorRights()
        {
            return _hasAdministratorRights;
        }

        public virtual bool HasGlobalQueryRights()
        {
            return _hasGlobalQueryRights;
        }

        public void SetExternalClientApplicationId(string id)
        {
            _externalClientApplicationId = id;
        }

        public void SetHasGlobalQueryRights(bool value)
        {
            _hasGlobalQueryRights = value;
        }
        public void SetHasAdmnistratorRights(bool value)
        {
            _hasAdministratorRights = value;
        }
    }
}
