﻿using System;
using System.Collections.Generic;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine
{
    internal sealed class CommandAllocatorPool : IDisposable
    {
        private readonly Queue<(CommandAllocator, long)> commandAllocatorQueue = new Queue<(CommandAllocator, long)>();

        public CommandAllocatorPool(GraphicsDevice device, CommandListType commandListType)
        {
            GraphicsDevice = device;
            CommandListType = commandListType;
        }

        public CommandListType CommandListType { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public void Dispose()
        {
            lock (commandAllocatorQueue)
            {
                foreach ((CommandAllocator commandAllocator, long _) in commandAllocatorQueue)
                {
                    commandAllocator.Dispose();
                }

                commandAllocatorQueue.Clear();
            }
        }

        public void Enqueue(CommandAllocator commandAllocator, long fenceValue)
        {
            lock (commandAllocatorQueue)
            {
                commandAllocatorQueue.Enqueue((commandAllocator, fenceValue));
            }
        }

        public CommandAllocator GetCommandAllocator()
        {
            lock (commandAllocatorQueue)
            {
                if (commandAllocatorQueue.Count > 0)
                {
                    (CommandAllocator commandAllocator, long fenceValue) = commandAllocatorQueue.Peek();

                    long completedValue;

                    switch (CommandListType)
                    {
                        case CommandListType.Direct:
                        case CommandListType.Bundle:
                            completedValue = GraphicsDevice.NativeFence.CompletedValue;
                            break;
                        case CommandListType.Copy:
                            completedValue = GraphicsDevice.NativeCopyFence.CompletedValue;
                            break;
                        default:
                            throw new ArgumentException("This command list type is not supported.");
                    }

                    if (fenceValue <= completedValue)
                    {
                        commandAllocatorQueue.Dequeue();
                        commandAllocator.Reset();

                        return commandAllocator;
                    }
                }

                return GraphicsDevice.NativeDevice.CreateCommandAllocator(CommandListType);
            }
        }
    }
}
