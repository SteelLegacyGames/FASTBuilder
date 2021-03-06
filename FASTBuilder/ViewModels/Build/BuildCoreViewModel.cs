﻿using System;
using Caliburn.Micro;
using FastBuilder.Communication.Events;
using FastBuilder.Services;

namespace FastBuilder.ViewModels.Build
{
	internal class BuildCoreViewModel : PropertyChangedBase
	{
		private bool _isBusy;
		private BuildJobViewModel _currentJob;
		private double _uiJobsTotalWidth;
		public int Id { get; }
		public BuildWorkerViewModel OwnerWorker { get; }

		public BindableCollection <BuildJobViewModel> Jobs { get; } = new BindableCollection <BuildJobViewModel>();

		public BuildJobViewModel CurrentJob
		{
			get => _currentJob;
			private set
			{
				if (object.Equals(value, _currentJob))
				{
					return;
				}

				_currentJob = value;
				this.NotifyOfPropertyChange();
			}
		}

		public bool IsBusy
		{
			get => _isBusy;
			private set
			{
				if (value == _isBusy)
				{
					return;
				}

				_isBusy = value;
				this.NotifyOfPropertyChange();
			}
		}

		public double UIJobsTotalWidth
		{
			get => _uiJobsTotalWidth;
			private set
			{
				if (value.Equals(_uiJobsTotalWidth))
				{
					return;
				}

				_uiJobsTotalWidth = value;
				this.NotifyOfPropertyChange();
			}
		}

		public BuildCoreViewModel(int id, BuildWorkerViewModel ownerWorker)
		{
			this.Id = id;
			this.OwnerWorker = ownerWorker;
			IoC.Get<IViewTransformService>().PreScalingChanging 
				+= this.ViewTransformService_PreScalingChanging;
		}

		private void ViewTransformService_PreScalingChanging(object sender, EventArgs e)
		{
			this.UpdateUIJobsTotalWidth();
		}

		public BuildJobViewModel OnJobFinished(FinishJobEventArgs e)
		{
			this.IsBusy = false;
			if (this.CurrentJob != null)
			{
				var job = this.CurrentJob;
				this.CurrentJob.OnFinished(e);
				this.CurrentJob = null;
				return job;
			}

			return null;
		}

		public BuildJobViewModel OnJobStarted(StartJobEventArgs e, DateTime sessionStartTime)
		{
			this.IsBusy = true;

			this.CurrentJob = new BuildJobViewModel(this, e, sessionStartTime);

			// called from log watcher thread
			lock (this.Jobs)
			{
				this.Jobs.Add(this.CurrentJob);
			}

			return this.CurrentJob;
		}

		public void OnSessionStopped(double currentTimeOffset)
		{
			foreach (var job in this.Jobs)
			{
				job.OnSessionStopped(currentTimeOffset);
			}
		}

		public void Tick(double currentTimeOffset)
		{
			// called from tick thread
			lock (this.Jobs)
			{
				foreach (var job in this.Jobs)
				{
					job.Tick(currentTimeOffset);
				}
			}

			this.UpdateUIJobsTotalWidth();
		}

		private void UpdateUIJobsTotalWidth()
		{
			this.UIJobsTotalWidth = IoC.Get<IViewTransformService>().Scaling *
			                        this.OwnerWorker.OwnerSession.ElapsedTime.TotalSeconds;
		}
	}
}
