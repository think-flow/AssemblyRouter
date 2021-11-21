using AssemblyRouter.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AssemblyRouter.ApplicationModels
{
    internal class AssemblyControllerActionDescriptorProvide
    {
        private const string ControllerTypeNameSuffix = "Controller";

        private static Func<ApplicationModel, IList<ControllerActionDescriptor>> _buildDescriptorDelegate;

        private static Func<object, IEnumerable<TypeInfo>, ApplicationModel> _createApplicationModel;

        private readonly object ApplicationModelFactoryInstance;

        static AssemblyControllerActionDescriptorProvide()
        {
            CreateCreateApplicationModelDelegate();
            CreateBuildDescriptorDelegate();
        }

        public AssemblyControllerActionDescriptorProvide(IServiceProvider serviceProvider)
        {
            ApplicationModelFactoryInstance = serviceProvider.GetService(ReflectionHelper.GetApplicationModelFactoryType());
        }

        public IReadOnlyList<ControllerActionDescriptor> GetDescriptor(IEnumerable<Assembly> applicationParts)
        {
            //1. 获取控制器的Typeinfo
            var typeInfos = applicationParts.SelectMany(assembly => assembly.DefinedTypes.Where(IsController));
            //2. 获取ApplicationModelFactory的实例，并反射调用CreateApplicationModel方法创建ApplicationModel
            var applicationModel = _createApplicationModel.Invoke(ApplicationModelFactoryInstance, typeInfos);
            //3. 反射调用ControllerActionDescriptorBuilder静态类的Build方法，构建ControllerActionDescriptor
            return (List<ControllerActionDescriptor>)_buildDescriptorDelegate.Invoke(applicationModel);
        }

        private bool IsController(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            // We only consider public top-level classes as controllers. IsPublic returns false for nested
            // classes, regardless of visibility modifiers
            if (!typeInfo.IsPublic)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.IsDefined(typeof(NonControllerAttribute)))
            {
                return false;
            }

            if (!typeInfo.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
                !typeInfo.IsDefined(typeof(ControllerAttribute)))
            {
                return false;
            }

            return true;
        }

        private static void CreateCreateApplicationModelDelegate()
        {
            var factoryParam = Expression.Parameter(typeof(object), "factory");
            var typeInfosParam = Expression.Parameter(typeof(IEnumerable<TypeInfo>), "typeInfos");

            //return ((ApplicationModelFactory)factory).CreateApplicationModel(typeInfos)
            var factoryType = ReflectionHelper.GetApplicationModelFactoryType();
            var callCreateApplicationModelm = Expression.Call(Expression.Convert(factoryParam, factoryType), factoryType.GetMethod("CreateApplicationModel"), typeInfosParam);

            _createApplicationModel = Expression.Lambda<Func<object, IEnumerable<TypeInfo>, ApplicationModel>>(callCreateApplicationModelm, factoryParam, typeInfosParam).Compile();
        }

        private static void CreateBuildDescriptorDelegate()
        {
            var applicationParam = Expression.Parameter(typeof(ApplicationModel), "application");

            //return ControllerActionDescriptorBuilder.Build(application);
            var controllerBuilderType = ReflectionHelper.GetControllerActionDescriptorBuilderType();
            var buildMethod = controllerBuilderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public);
            var callBuildm = Expression.Call(buildMethod, applicationParam);

            _buildDescriptorDelegate = Expression.Lambda<Func<ApplicationModel, IList<ControllerActionDescriptor>>>(callBuildm, applicationParam).Compile();
        }
    }
}
