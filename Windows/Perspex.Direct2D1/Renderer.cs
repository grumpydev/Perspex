﻿// -----------------------------------------------------------------------
// <copyright file="Renderer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using System.Linq;
    using Perspex.Direct2D1.Media;
    using Perspex.Media;
    using Perspex.Platform;
    using SharpDX;
    using SharpDX.Direct2D1;
    using Splat;
    using DwFactory = SharpDX.DirectWrite.Factory;
    using Matrix = Perspex.Matrix;
    using Point = Perspex.Point;

    public class Renderer : IRenderer
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private RenderTarget renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public Renderer(IntPtr hwnd, double width, double height)
        {
            this.Direct2DFactory = Locator.Current.GetService<Factory>();
            this.DirectWriteFactory = Locator.Current.GetService<DwFactory>();

            RenderTargetProperties renderTargetProperties = new RenderTargetProperties
            {
            };

            HwndRenderTargetProperties hwndProperties = new HwndRenderTargetProperties
            {
                Hwnd = hwnd,
                PixelSize = new Size2((int)width, (int)height),
                PresentOptions = PresentOptions.Immediately,
            };

            this.renderTarget = new WindowRenderTarget(
                this.Direct2DFactory,
                renderTargetProperties,
                hwndProperties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public Renderer(RenderTarget renderTarget)
        {
            this.Direct2DFactory = Locator.Current.GetService<Factory>();
            this.DirectWriteFactory = Locator.Current.GetService<DwFactory>();
            this.renderTarget = renderTarget;
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public DwFactory DirectWriteFactory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of times <see cref="Render"/> has been called.
        /// </summary>
        public int RenderCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">Unused.</param>
        public void Render(IVisual visual, IPlatformHandle handle)
        {
            using (DrawingContext context = new DrawingContext(this.renderTarget, this.DirectWriteFactory))
            {
                this.Render(visual, context, Matrix.Identity, Matrix.Identity);
            }

            ++this.RenderCount;
        }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            WindowRenderTarget window = this.renderTarget as WindowRenderTarget;

            if (window == null)
            {
                throw new InvalidOperationException(string.Format(
                    "A renderer with a target of type '{0}' cannot be resized.",
                    this.renderTarget.GetType().Name));
            }

            window.Resize(new Size2(width, height));
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        private void Render(IVisual visual, DrawingContext context, Matrix translation, Matrix transform)
        {
            if (visual.IsVisible && visual.Opacity > 0)
            {
                // Translate any existing transform into this controls coordinate system.
                Matrix offset = Matrix.Translation(visual.Bounds.Position);
                transform = offset * transform * -offset;

                // Update the current offset.
                translation *= Matrix.Translation(visual.Bounds.Position);

                // Apply the control's render transform, if any.
                if (visual.RenderTransform != null)
                {
                    offset = Matrix.Translation(visual.TransformOrigin.ToPixels(visual.Bounds.Size));
                    transform *= -offset * visual.RenderTransform.Value * offset;
                }

                // Draw the control and its children.
                var m = transform * translation;
                var d = context.PushTransform(m);

                using (visual.ClipToBounds ? context.PushClip(visual.Bounds) : null)
                {
                    visual.Render(context);
                    d.Dispose();

                    foreach (var child in visual.VisualChildren)
                    {
                        this.Render(child, context, translation, transform);
                    }
                }
            }
        }
    }
}
