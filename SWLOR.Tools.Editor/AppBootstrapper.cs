using Autofac;
using Caliburn.Micro;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.Startup;
using SWLOR.Tools.Editor.Startup.Contracts;
using SWLOR.Tools.Editor.ViewModels;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using IContainer = Autofac.IContainer;
using System.Windows.Controls;
using SWLOR.Tools.Editor.Factories;
using SWLOR.Tools.Editor.Factories.Contracts;
using SWLOR.Tools.Editor.Helpers;
using Newtonsoft.Json;

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
            _container.Resolve<IEventAggregator>().PublishOnUIThread(new ApplicationStartedMessage());
        }

        protected override void OnExit(object sender, EventArgs args)
        {
            _container.Resolve<IEventAggregator>().PublishOnUIThread(new ApplicationEndedMessage());

            var settings = _container.Resolve<AppSettings>();
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText("./Settings.json", json);
        }

        protected virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Singletons
            builder.RegisterType<AppSettings>().SingleInstance();

            // Startables
            builder.RegisterType<CreateDataDirectories>().As<IStartable>().SingleInstance();
            builder.RegisterType<InitializeJsonSerializer>().As<IStartable>().SingleInstance();
            
            // Other Startup Events
            builder.RegisterType<PostBootstrap>().As<IPostBootstrap>().SingleInstance();

            // Factories
            builder.RegisterType<DataContextFactory>().As<IDataContextFactory>().SingleInstance();

            // View Models
            builder.RegisterType<DatabaseConnectionViewModel>().As<IDatabaseConnectionViewModel>();
            builder.RegisterType<DataSyncViewModel>().As<IDataSyncViewModel>();
            builder.RegisterType<EditorListViewModel>().As<IEditorListViewModel>();
            builder.RegisterType<ErrorViewModel>().As<IErrorViewModel>();
            builder.RegisterType<LootEditorViewModel>().As<ILootEditorViewModel>();
            builder.RegisterType<MenuBarViewModel>().As<IMenuBarViewModel>();
            builder.RegisterType(typeof(ObjectListViewModel<LootTable>)).As<IObjectListViewModel<LootTable>>();

            // Shell
            builder.RegisterType<ShellViewModel>().As<IShellViewModel>();
        }
    }
}