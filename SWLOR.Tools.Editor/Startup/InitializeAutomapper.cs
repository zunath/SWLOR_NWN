using System;
using Autofac;
using AutoMapper;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeAutomapper: IStartable
    {
        public void Start()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<LootTableViewModel, LootTableViewModel>();
                cfg.CreateMap<LootTableItemViewModel, LootTableItemViewModel>();

            });
        }
    }
}
