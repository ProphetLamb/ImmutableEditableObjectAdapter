﻿//----------------------
// <auto-generated>
//	Generated by the ViewModelGenTool_3 v3. DO NOT EDIT!
//	Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//----------------------
#pragma warning disable

using global::System;
using global::System.Linq;
using global::System.Threading.Tasks;

namespace ImmutableEditableObjectAdapter.Samples.Uno.Presentation
{


	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("ViewModelGenTool_3", "3")]
	[global::Uno.Extensions.Reactive.Bindings.Bindable(typeof(global::ImmutableEditableObjectAdapter.Samples.Uno.Presentation.SecondModel))]
	public partial class SecondViewModel : global::Uno.Extensions.Reactive.Bindings.BindableViewModelBase
	{



		public SecondViewModel(global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Entity Entity)
			: this(new global::ImmutableEditableObjectAdapter.Samples.Uno.Presentation.SecondModel(Entity))
		{
			if (global::Uno.Extensions.Reactive.Config.FeedConfiguration.EffectiveHotReload.HasFlag(global::Uno.Extensions.Reactive.Config.HotReloadSupport.State))
			{
				__reactiveModelArgs = new (Type, string, object?)[] { (typeof(global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Entity), "Entity", Entity as object) };
			}
		}

		protected SecondViewModel(global::ImmutableEditableObjectAdapter.Samples.Uno.Presentation.SecondModel model)
		{
			var ctx = global::Uno.Extensions.Reactive.Core.SourceContext.GetOrCreate(model);

			// Share the context between Model and ViewModel
				global::Uno.Extensions.Reactive.Core.SourceContext.Set(this, ctx);
				base.RegisterDisposable(model);
				Model = model;

			model.__reactiveBindableViewModel = this;




			if (model is global::System.ComponentModel.INotifyPropertyChanged npc)
			{
				npc.PropertyChanged += __Reactive_OnModelPropertyChanged;
			}
		}

		#region Hot-reload support
		private (Type type, string name, object? value)[]? __reactiveModelArgs;

		protected override (Type type, string name, object? value)[] __Reactive_GetModelArguments()
			=> __reactiveModelArgs ?? base.__Reactive_GetModelArguments();

		#if True
		protected override void __Reactive_UpdateModel(object updatedModel)
		{
			if (Model is global::System.ComponentModel.INotifyPropertyChanged npc)
			{
				npc.PropertyChanged -= __Reactive_OnModelPropertyChanged;
			}

			var previousModel = (object)Model;

			__Reactive_BindableInitializeForUpdatedModel(updatedModel, global::Uno.Extensions.Reactive.Core.SourceContext.GetOrCreate(updatedModel));
			__Reactive_TryPatchBindableProperties(previousModel, updatedModel);

			base.RaisePropertyChanged(""); // 'Model' and any other mapped property.
		}
		#endif

		protected virtual void __Reactive_BindableInitializeForUpdatedModel(object updatedModel, global::Uno.Extensions.Reactive.Core.SourceContext ctx)
		{
			#if False
			base.__Reactive_BindableInitializeForUpdatedModel(updatedModel, ctx);
			#else
			//Model = model;
			#endif

			dynamic model = updatedModel;

			model.__reactiveBindableViewModel = this;

			try
			{

			}
			catch (Exception)
			{
				if (__Reactive_Log().IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
				{
					global::Microsoft.Extensions.Logging.LoggerExtensions.Log(
						__Reactive_Log(),
						global::Microsoft.Extensions.Logging.LogLevel.Warning,
						$"Failed to initialize 'Entity' from the updated model, this member is unlikely to work properly.");
				}
			}


			if (model is global::System.ComponentModel.INotifyPropertyChanged npc)
			{
				npc.PropertyChanged += __Reactive_OnModelPropertyChanged;
			}
		}
		#endregion

		private void __Reactive_OnModelPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs args)
			=> base.RaisePropertyChanged(args.PropertyName);

		public global::ImmutableEditableObjectAdapter.Samples.Uno.Presentation.SecondModel Model { get; private set; }

		public global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Entity Entity
		{
			get => Model.Entity;
		}

	}

}