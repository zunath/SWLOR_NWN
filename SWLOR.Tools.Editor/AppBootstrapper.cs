using Autofac;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Helpers;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.Startup;
using SWLOR.Tools.Editor.Startup.Contracts;
using SWLOR.Tools.Editor.ViewModels;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading;
using IContainer = Autofac.IContainer;
using SWLOR.Tools.Editor.ViewModels.Data;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Tools.Editor
{
    public class AppBootstrapper : BootstrapperBase
    {
        private IContainer _container;

        public AppBootstrapper()
        {
            Initialize();

            ConventionManager.AddElementConvention<PasswordBox>(
                PasswordBoxHelper.BoundPasswordProperty,
                "Password",
                "PasswordChanged");
        }

        protected override void Configure()
        {
            var builder = new ContainerBuilder();

            //  register view models
            builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
              //  must be a type that ends with ViewModel
              .Where(type => type.Name.EndsWith("ViewModel"))
              //  must be in a namespace ending with ViewModels
              .Where(type => !(string.IsNullOrWhiteSpace(type.Namespace)) && type.Namespace.EndsWith("ViewModels"))
              //  must implement INotifyPropertyChanged (deriving from PropertyChangedBase will statisfy this)
              .Where(type => type.GetInterface(typeof(INotifyPropertyChanged).Name) != null)
              //  registered as self
              .AsSelf()
              //  always create a new one
              .InstancePerDependency();

            //  register views
            builder.RegisterAssemblyTypes(AssemblySource.Instance.ToArray())
              //  must be a type that ends with View
              .Where(type => type.Name.EndsWith("View"))
              //  must be in a namespace that ends in Views
              .Where(type => !(string.IsNullOrWhiteSpace(type.Namespace)) && type.Namespace.EndsWith("Views"))
              //  registered as self
              .AsSelf()
              //  always create a new one
              .InstancePerDependency();


            //  register the single window manager for this container
            builder.Register<IWindowManager>(c => new WindowManager()).InstancePerLifetimeScope();
            //  register the single event aggregator for this container
            builder.Register<IEventAggregator>(c => new EventAggregator()).InstancePerLifetimeScope();

            ConfigureContainer(builder);

            _container = builder.Build();
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                if (_container.IsRegistered(serviceType))
                    return _container.Resolve(serviceType);
            }
            else
            {
                if (_container.IsRegisteredWithKey(key, serviceType))
                    return _container.ResolveKeyed(key, serviceType);
            }
            throw new Exception(string.Format("Could not locate any instances of contract {0}.", key ?? serviceType.Name));
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return _container.Resolve(typeof(IEnumerable<>).MakeGenericType(serviceType)) as IEnumerable<object>;
        }

        protected override void BuildUp(object instance)
        {
            _container.InjectProperties(instance);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<IShellViewModel>();

            _container.Resolve<IPostBootstrap>().RunStartUp();
            _container.Resolve<IEventAggregator>().PublishOnUIThread(new ApplicationStarted());
        }

        protected override void OnExit(object sender, EventArgs args)
        {
            _container.Resolve<IEventAggregator>().PublishOnUIThread(new ApplicationEnded());

            var settings = _container.Resolve<AppSettings>();
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText("./Settings.json", json);
        }

        protected virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // SWLOR.Game.Server Registrations
            builder.RegisterType<DatabaseBackgroundThread>().As<IDatabaseThread>().SingleInstance();

            // Singletons
            builder.RegisterType<AppSettings>().SingleInstance();

            // Startables
            builder.RegisterType<CreateDataDirectories>().As<IStartable>().SingleInstance();
            builder.RegisterType<InitializeJsonSerializer>().As<IStartable>().SingleInstance();
            builder.RegisterType<InitializeAutomapper>().As<IStartable>().SingleInstance();
            builder.RegisterType<BackgroundThreadManager>().As<IBackgroundThreadManager>().SingleInstance();

            // Other Startup Events
            builder.RegisterType<PostBootstrap>().As<IPostBootstrap>().SingleInstance();
            
            // View Models
            builder.RegisterType<ApartmentBuildingEditorViewModel>().As<IApartmentBuildingEditorViewModel>();
            builder.RegisterType<CustomEffectEditorViewModel>().As<ICustomEffectEditorViewModel>();
            builder.RegisterType<DatabaseConnectionViewModel>().As<IDatabaseConnectionViewModel>();
            builder.RegisterType<DataSyncViewModel>().As<IDataSyncViewModel>();
            builder.RegisterType<DownloadEditorViewModel>().As<IDownloadEditorViewModel>();
            builder.RegisterType<EditorListViewModel>().As<IEditorListViewModel>();
            builder.RegisterType<ErrorViewModel>().As<IErrorViewModel>();
            builder.RegisterType<ImportViewModel>().As<IImportViewModel>();
            builder.RegisterType<ExportViewModel>().As<IExportViewModel>();
            builder.RegisterType<KeyItemEditorViewModel>().As<IKeyItemEditorViewModel>();
            builder.RegisterType<LootEditorViewModel>().As<ILootEditorViewModel>();
            builder.RegisterType<MenuBarViewModel>().As<IMenuBarViewModel>();
            builder.RegisterType<PlantEditorViewModel>().As<IPlantEditorViewModel>();
            builder.RegisterType<RenameObjectViewModel>().As<IRenameObjectViewModel>();
            builder.RegisterType<YesNoViewModel>().As<IYesNoViewModel>();

            // Object list view models
            builder.RegisterType(typeof(ObjectListViewModel<ApartmentBuildingViewModel>)).As<IObjectListViewModel<ApartmentBuildingViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<BaseStructureViewModel>)).As<IObjectListViewModel<BaseStructureViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<BuildingStyleViewModel>)).As<IObjectListViewModel<BuildingStyleViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<CooldownCategoryViewModel>)).As<IObjectListViewModel<CooldownCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<CraftBlueprintCategoryViewModel>)).As<IObjectListViewModel<CraftBlueprintCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<CraftBlueprintViewModel>)).As<IObjectListViewModel<CraftBlueprintViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<CraftDeviceViewModel>)).As<IObjectListViewModel<CraftDeviceViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<CustomEffectViewModel>)).As<IObjectListViewModel<CustomEffectViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<DownloadViewModel>)).As<IObjectListViewModel<DownloadViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<FameRegionViewModel>)).As<IObjectListViewModel<FameRegionViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<GameTopicCategoryViewModel>)).As<IObjectListViewModel<GameTopicCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<GameTopicViewModel>)).As<IObjectListViewModel<GameTopicViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<KeyItemCategoryViewModel>)).As<IObjectListViewModel<KeyItemCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<KeyItemViewModel>)).As<IObjectListViewModel<KeyItemViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<LootTableViewModel>)).As<IObjectListViewModel<LootTableViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<ModViewModel>)).As<IObjectListViewModel<ModViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<NPCGroupViewModel>)).As<IObjectListViewModel<NPCGroupViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<PerkCategoryViewModel>)).As<IObjectListViewModel<PerkCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<PerkViewModel>)).As<IObjectListViewModel<PerkViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<PlantViewModel>)).As<IObjectListViewModel<PlantViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<QuestViewModel>)).As<IObjectListViewModel<QuestViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<SkillCategoryViewModel>)).As<IObjectListViewModel<SkillCategoryViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<SkillViewModel>)).As<IObjectListViewModel<SkillViewModel>>();
            builder.RegisterType(typeof(ObjectListViewModel<SpawnViewModel>)).As<IObjectListViewModel<SpawnViewModel>>();

            // Shell
            builder.RegisterType<ShellViewModel>().As<IShellViewModel>();
        }
    }
}