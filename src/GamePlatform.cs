#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.IO;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
#endregion
namespace 
Microsoft.Xna.Framework
{
	abstract class GamePlatform : IDisposable
	{
		#region Public Properties

		/// <summary>
		/// Gets the Game instance that owns this GamePlatform instance.
		/// </summary>
		public Game Game
		{
			get;
			private set;
		}

		public bool IsActive
		{
			get
			{
				return _isActive;
			}
			internal set
			{
				if (_isActive != value)
				{
					_isActive = value;
					Raise(_isActive ? Activated : Deactivated, EventArgs.Empty);
				}
			}
		}

		public bool IsMouseVisible
		{
			get
			{
				return _isMouseVisible;
			}
			set
			{
				if (_isMouseVisible != value)
				{
					_isMouseVisible = value;
					OnIsMouseVisibleChanged();
				}
			}
		}

		public GameWindow Window
		{
			get
			{
				return _window;
			}
			protected set
			{
				if (_window == null)
				{
					Mouse.WindowHandle = value.Handle;
                    TouchPanel.PrimaryWindow = value;
				}

				_window = value;
			}
		}

		#endregion

		#region Internal Properties

		internal string OSVersion
		{
			get;
			private set;
		}

		#endregion

		#region Protected Properties

		protected bool IsDisposed
		{
			get
			{
				return disposed;
			}
		}

		#endregion

		#region Protected Variables

		protected TimeSpan _inactiveSleepTime = TimeSpan.FromMilliseconds(20.0);
		protected bool _needsToResetElapsedTime = false;

		#endregion

		#region Private Variables

		bool disposed;
		private bool _isActive;
		private bool _isMouseVisible;
		private GameWindow _window;

		#endregion

		#region Protected Constructor

		protected GamePlatform(Game game, string osVersion)
		{
			if (game == null)
			{
				throw new ArgumentNullException("game");
			}
			Game = game;
			OSVersion = osVersion;
		}

		#endregion

		#region Deconstructor

		~GamePlatform()
		{
			Dispose(false);
		}

		#endregion

		#region Events

		public event EventHandler<EventArgs> Activated;
		public event EventHandler<EventArgs> Deactivated;

		#endregion

		#region Public Methods

		/// <summary>
		/// Gives derived classes an opportunity to do work before any
		/// components are initialized. Note that the base implementation sets
		/// IsActive to true, so derived classes should either call the base
		/// implementation or set IsActive to true by their own means.
		/// </summary>
		public virtual void BeforeInitialize()
		{
			IsActive = true;
			if (this.Game.GraphicsDevice == null)
			{
				IGraphicsDeviceManager graphicsDeviceManager = Game.Services.GetService(
					typeof(IGraphicsDeviceManager)
				) as IGraphicsDeviceManager;

				graphicsDeviceManager.CreateDevice();
			}
		}

		/// <summary>
		/// Gives derived classes an opportunity to do work just before the
		/// run loop is begun. Implementations may also return false to prevent
		/// the run loop from starting.
		/// </summary>
		/// <returns></returns>
		public virtual bool BeforeRun()
		{
			return true;
		}

		/// <summary>
		/// When implemented in a derived class, ends the active run loop.
		/// </summary>
		public abstract void Exit();

		/// <summary>
		/// When implemented in a derived class, starts the run loop and blocks
		/// until it has ended.
		/// </summary>
		public abstract void RunLoop();

		/// <summary>
		/// Gives derived classes an opportunity to do work just before Update
		/// is called for all IUpdatable components. Returning false from this
		/// method will result in this round of Update calls being skipped.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <returns></returns>
		public abstract bool BeforeUpdate(GameTime gameTime);

		/// <summary>
		/// Gives derived classes an opportunity to do work just before Draw
		/// is called for all IDrawable components. Returning false from this
		/// method will result in this round of Draw calls being skipped.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <returns></returns>
		public abstract bool BeforeDraw(GameTime gameTime);

		/// <summary>
		/// Gives derived classes an opportunity to modify
		/// Game.TargetElapsedTime before it is set.
		/// </summary>
		/// <param name="value">The proposed new value of TargetElapsedTime.</param>
		/// <returns>The new value of TargetElapsedTime that will be set.</returns>
		public virtual TimeSpan TargetElapsedTimeChanging(TimeSpan value)
		{
			return value;
		}

		/// <summary>
		/// Starts a device transition (windowed to full screen or vice versa).
		/// </summary>
		/// <param name='willBeFullScreen'>
		/// Specifies whether the device will be in full-screen mode upon completion of
		/// the change.
		/// </param>
		public abstract void BeginScreenDeviceChange(
			bool willBeFullScreen
		);

		/// <summary>
		/// Completes a device transition.
		/// </summary>
		/// <param name='screenDeviceName'>
		/// Screen device name.
		/// </param>
		/// <param name='clientWidth'>
		/// The new width of the game's client window.
		/// </param>
		/// <param name='clientHeight'>
		/// The new height of the game's client window.
		/// </param>
		public abstract void EndScreenDeviceChange(
			string screenDeviceName,
			int clientWidth,
			int clientHeight
		);

		/// <summary>
		/// Gives derived classes an opportunity to take action after
		/// Game.TargetElapsedTime has been set.
		/// </summary>
		public virtual void TargetElapsedTimeChanged() {}

		/// <summary>
		/// MSDN: Use this method if your game is recovering from a slow-running state, and
		/// ElapsedGameTime is too large to be useful. Frame timing is generally handled
		/// by the Game class, but some platforms still handle it elsewhere. Once all
		/// platforms rely on the Game class's functionality, this method and any overrides
		/// should be removed.
		/// </summary>
		public virtual void ResetElapsedTime() {}

		protected virtual void OnIsMouseVisibleChanged() {}

		public virtual void Present() {}

		public abstract void ShowRuntimeError(
			String title,
			String message
		);

		#endregion

		#region Internal Methods

		internal abstract DisplayMode GetCurrentDisplayMode();

		internal abstract DisplayModeCollection GetDisplayModes();

		internal abstract void SetPresentationInterval(PresentInterval interval);

		internal abstract bool HasTouch();

		internal abstract void TextureDataFromStream(
			Stream stream,
			out int width,
			out int height,
			out byte[] pixels,
			int reqWidth = -1,
			int reqHeight = -1,
			bool zoom = false
		);

		internal abstract void SavePNG(
			Stream stream,
			int width,
			int height,
			int imgWidth,
			int imgHeight,
			byte[] data
		);

		internal abstract Keys GetKeyFromScancode(Keys scancode);

		#endregion

		#region Private Methods

		private void Raise<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs e)
			where TEventArgs : EventArgs
		{
			if (handler != null)
			{
				handler(this, e);
			}
		}

		#endregion

		#region Public Static Methods

		public static GamePlatform Create(Game game)
		{
			/* I suspect you may have an urge to put an #if in here for new
			 * GamePlatform implementations.
			 *
			 * DON'T.
			 *
			 * Determine this at runtime, or load dynamically.
			 * No amount of whining will get me to budge on this.
			 * -flibit
			 */
			return new SDL2_GamePlatform(game);
		}

		#endregion

		#region IDisposable implementation

		/// <summary>
		/// Performs application-defined tasks associated with freeing,
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				Mouse.WindowHandle = IntPtr.Zero;
                TouchPanel.PrimaryWindow = null;

				disposed = true;
			}
		}

		/// <summary>
		/// Log the specified Message.
		/// </summary>
		/// <param name='Message'>
		///
		/// </param>
		[System.Diagnostics.Conditional("DEBUG")]
		public virtual void Log(string Message) {}

		#endregion
	}
}
