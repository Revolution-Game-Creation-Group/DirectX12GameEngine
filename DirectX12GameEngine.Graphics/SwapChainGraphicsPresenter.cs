﻿using System;
using DirectX12GameEngine.Core;
using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Resource = SharpDX.Direct3D12.Resource;

namespace DirectX12GameEngine.Graphics
{
    public sealed class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private const int BufferCount = 2;

        private readonly Texture[] renderTargets = new Texture[BufferCount];

        private readonly SwapChain3 swapChain;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            swapChain = CreateSwapChain();

            if (GraphicsDevice.RenderTargetViewAllocator.DescriptorHeap.Description.DescriptorCount != BufferCount)
            {
                GraphicsDevice.RenderTargetViewAllocator.Dispose();
                GraphicsDevice.RenderTargetViewAllocator = new DescriptorAllocator(GraphicsDevice, DescriptorHeapType.RenderTargetView, descriptorCount: BufferCount);
            }

            CreateRenderTargets();
        }

        public override Texture BackBuffer => renderTargets[swapChain.CurrentBackBufferIndex];

        public override object NativePresenter => swapChain;

        public override void Dispose()
        {
            swapChain.Dispose();

            foreach (Texture renderTarget in renderTargets)
            {
                renderTarget.Dispose();
            }

            base.Dispose();
        }

        private SwapChain3 CreateSwapChain()
        {
            SwapChainDescription1 swapChainDescription = new SwapChainDescription1
            {
                Width = PresentationParameters.BackBufferWidth,
                Height = PresentationParameters.BackBufferHeight,
                SampleDescription = new SampleDescription(1, 0),
                Stereo = PresentationParameters.Stereo,
                Usage = Usage.RenderTargetOutput,
                BufferCount = BufferCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Format = PresentationParameters.BackBufferFormat,
                Flags = SwapChainFlags.None,
                AlphaMode = AlphaMode.Unspecified
            };

            SwapChain3 swapChain;

            switch (PresentationParameters.DeviceWindowHandle.ContextType)
            {
                case AppContextType.CoreWindow:
                    using (Factory4 factory = new Factory4())
                    using (ComObject window = new ComObject(PresentationParameters.DeviceWindowHandle.NativeWindow))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, window, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                    }
                    break;
                case AppContextType.Xaml:
                    swapChainDescription.AlphaMode = AlphaMode.Premultiplied;

                    using (Factory4 factory = new Factory4())
                    using (ISwapChainPanelNative nativePanel = ComObject.As<ISwapChainPanelNative>(PresentationParameters.DeviceWindowHandle.NativeWindow))
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                        nativePanel.SwapChain = swapChain;
                    }
                    break;
                case AppContextType.WinForms:
                    using (Factory4 factory = new Factory4())
                    using (SwapChain1 tempSwapChain = new SwapChain1(factory, GraphicsDevice.NativeCommandQueue, PresentationParameters.DeviceWindowHandle.Handle, ref swapChainDescription))
                    {
                        swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                    }
                    break;
                default:
                    throw new NotSupportedException("This app context type is not supported while creating a swap chain.");
            }

            return swapChain;
        }

        public override void Present()
        {
            swapChain.Present(PresentationParameters.SyncInterval, PresentFlags.None, PresentationParameters.PresentParameters);
        }

        protected override void ResizeBackBuffer(int width, int height)
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i].Dispose();
            }

            swapChain.ResizeBuffers(BufferCount, width, height, PresentationParameters.BackBufferFormat, SwapChainFlags.None);

            CreateRenderTargets();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height)
        {
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        private void CreateRenderTargets()
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargets[i] = new Texture(GraphicsDevice, swapChain.GetBackBuffer<Resource>(i), DescriptorHeapType.RenderTargetView);
            }
        }
    }
}
