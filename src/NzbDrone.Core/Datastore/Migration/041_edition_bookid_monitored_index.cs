using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(41)]
    public class EditionBookIdMonitoredIndex : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("Editions").OnColumn("BookId").Ascending().OnColumn("Monitored").Ascending();
        }
    }
}
