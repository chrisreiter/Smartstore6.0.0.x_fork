using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Stores
{
    [LocalizedDisplay("Admin.System.Tenants.Fields.")]
    public partial class TenantModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Domain")]
        public string Domain { get; set; }

        [LocalizedDisplay("*DatabaseSchema")]
        public string DatabaseSchema { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*UpdatedOn")]
        public DateTime UpdatedOn { get; set; }

        public int StoreCount { get; set; }
    }
}