using System;
using Autofac;
using AutoMapper;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeAutomapper: IStartable
    {
        public void Start()
        {
            Mapper.Initialize(cfg =>
            {

            });
        }
    }
}
