﻿using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp.RichText;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if ANDROID
using System;
using Microsoft.Xna.Framework.GamerServices;
#endif
#elif STRIDE
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Core.Mathematics;
using Stride.Input;
using Texture2D = Stride.Graphics.Texture;
using SharpDX.Direct3D11;
#endif

namespace FontStashSharp.Samples
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		// private const string Text = @"This text is split into 3 lines./nFirst break is explicit through command '//n'. Second break is automatic since Width is set.";
		// private const string Text = "First line./nSecond line.";
		// private const string Text = "This is /c[red]colored /c[#00f0fa]ext, /cdcolor could be set either /c[lightGreen]by name or /c[#fa9000ff]by hex code.";
		// private const string Text = @"Text in default font./n/f[arialbd.ttf, 24]Bold and smaller font. /f[ariali.ttf, 48]Italic and larger font./n/fdBack to the default font.";
		// private const string Text = @"E=mc/v[-8]2/n/vdMass–energy equivalence.";
		// private const string Text = @"A small tree: /i[mangrove1.png]";
		private const string Text = @"A small /c[red]tree: /v[8]/i[mangrove1.png]";
		private readonly static int? Width = null;

#if !STRIDE
		private readonly GraphicsDeviceManager _graphics;
#endif

		public static Game1 Instance { get; private set; }

		private SpriteBatch _spriteBatch;
		private Texture2D _white;
		private bool _animatedScaling = false;
		private float _angle;
		private RichTextLayout _formattedText;
		private readonly Dictionary<string, FontSystem> _fontCache = new Dictionary<string, FontSystem>();
		private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

		public Game1()
		{
			Instance = this;

#if MONOGAME || FNA
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};

			Window.AllowUserResizing = true;
#endif

			IsMouseVisible = true;
		}

#if STRIDE
		public override void ConfirmRenderingSettings(bool gameCreation)
		{
			base.ConfirmRenderingSettings(gameCreation);

			GraphicsDeviceManager.PreferredBackBufferWidth = 1200;
			GraphicsDeviceManager.PreferredBackBufferHeight = 800;
			GraphicsDeviceManager.PreferredColorSpace = ColorSpace.Gamma;
		}
#endif

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
#if !STRIDE
		protected override void LoadContent()
#else
		protected override Task LoadContent()
#endif
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// TODO: use this.Content to load your game content here

			var fontSystem = new FontSystem();
			fontSystem.AddFont(File.ReadAllBytes(@"C:/Windows/Fonts/arial.ttf"));

			_formattedText = new RichTextLayout
			{
				Font = fontSystem.GetFont(32),
				Text = Text,
				Width = Width
			};

			RichTextDefaults.FontResolver = p =>
			{
				// Parse font name and size
				var args = p.Split(',');
				var fontName = args[0].Trim();
				var fontSize = int.Parse(args[1].Trim());

				// _fontCache is field of type Dictionary<string, FontSystem>
				// It is used to cache fonts
				FontSystem fontSystem;
				if (!_fontCache.TryGetValue(fontName, out fontSystem))
				{
					// Load and cache the font system
					fontSystem = new FontSystem();
					fontSystem.AddFont(File.ReadAllBytes(@$"C:\Windows\Fonts\{fontName}"));
					_fontCache[fontName] = fontSystem;
				}

				// Return the required font
				return fontSystem.GetFont(fontSize);
			};

			RichTextDefaults.ImageResolver = p =>
			{
				Texture2D texture;

				// _textureCache is field of type Dictionary<string, Texture2D>
				// it is used to cache textures
				if (!_textureCache.TryGetValue(p, out texture))
				{
					using (var stream = File.OpenRead(@"D:\Temp\DCSSTiles\dngn\trees\" + p))
					{
						texture = Texture2D.FromStream(GraphicsDevice, stream);
					}

					_textureCache[p] = texture;
				}

				return new TextureFragment(texture);
			};

#if MONOGAME || FNA
			_white = new Texture2D(GraphicsDevice, 1, 1);
			_white.SetData(new[] { Color.White });
#elif STRIDE
			_white = Texture2D.New2D(GraphicsDevice, 1, 1, false, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource);
			_white.SetData(GraphicsContext.CommandList, new[] { Color.White } );
#endif

			GC.Collect();

#if STRIDE
			return base.LoadContent();
#endif
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			KeyboardUtils.Begin();

			if (KeyboardUtils.IsPressed(Keys.Enter))
			{
//				_currentFontSystem.UseKernings = !_currentFontSystem.UseKernings;
			}

			if (KeyboardUtils.IsPressed(Keys.LeftShift))
			{
				_animatedScaling = !_animatedScaling;
			}

			KeyboardUtils.End();
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
#if MONOGAME || FNA
			GraphicsDevice.Clear(Color.CornflowerBlue);
			TimeSpan total = gameTime.TotalGameTime;
#elif STRIDE
			// Clear screen
			GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.CornflowerBlue);
			GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);

			// Set render target
			GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
			TimeSpan total = gameTime.Total;
#endif

			// TODO: Add your drawing code here
#if MONOGAME || FNA
			_spriteBatch.Begin();
#elif STRIDE
			_spriteBatch.Begin(GraphicsContext);
#endif

			Vector2 scale = _animatedScaling
				? new Vector2(1 + .25f * (float)Math.Sin(total.TotalSeconds * .5f))
				: Vector2.One;

#if !STRIDE
			var viewportSize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
#else
			var viewportSize = new Point(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
#endif
			var position = new Vector2(0, viewportSize.Y / 2);

			var rads = (float)(_angle * Math.PI / 180);
			// var normalizedOrigin = new Vector2(0.5f, 0.5f);
			var normalizedOrigin = Vector2.Zero;

			var size = _formattedText.Size;
/*			_spriteBatch.Draw(_white, new Rectangle((int)position.X, (int)position.Y, size.X, size.Y),
				null, Color.Green, rads, normalizedOrigin, SpriteEffects.None, 0.0f);*/

			var origin = new Vector2(_formattedText.Size.X / 2.0f, _formattedText.Size.Y / 2.0f);

			_formattedText.Width = GraphicsDevice.Viewport.Width;
			_formattedText.Draw(_spriteBatch, position, Color.White);

			_spriteBatch.End();

//			_angle += 0.4f;

			while (_angle >= 360.0f)
			{
				_angle -= 360.0f;
			}

			base.Draw(gameTime);
		}
	}
}