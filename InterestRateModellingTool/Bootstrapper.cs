using System.Linq;
using Bristotti.Finance.DataAccess;
using Bristotti.Finance.Model;
using Castle.Windsor;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;

namespace InterestRateModellingTool
{
    public class BootStrapper
    {
        public static ISessionFactory SessionFactory { get; private set; }
        public static IWindsorContainer Container { get; private set; }

        public static void Init()
        {
            var cfg = new Configuration();

            cfg.Configure();

            var mapper = new ConventionModelMapper();

            var entityType = typeof(Entity);
            mapper.IsEntity(
                (t, declared) => entityType.IsAssignableFrom(t) && entityType != t && !t.IsInterface);
            mapper.IsRootEntity((t, declared) => entityType == t.BaseType);

            mapper.Class<Entity>(map =>
            {
                map.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
                map.Version(x => x.Version, m => m.Generated(VersionGeneration.Always));
            });
            mapper.BeforeMapProperty += (insp, prop, map) => map.NotNullable(true);

            mapper.Class<CopomMeeting>(map => map.Property(x => x.InterestTarget, pm => pm.NotNullable(false)));

            var mapping = mapper.CompileMappingFor(
                entityType.Assembly.GetExportedTypes()
                    .Where(t => t.Namespace.EndsWith("Model")));
            cfg.AddMapping(mapping);
            
            SessionFactory = cfg.BuildSessionFactory();

            new SchemaExport(cfg).Execute(true, true, false);

            var container = new WindsorContainer();
            Container = container;
        }
    }
}
