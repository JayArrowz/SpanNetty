﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace DotNetty.Buffers.Tests
{
    public class BufferUtilities
    {
        public static ReadOnlySequence<T> CreateSplitBuffer<T>(T[] buffer, int minSize, int maxSize) where T : struct
        {
            if (buffer == null || buffer.Length == 0 || minSize <= 0 || maxSize <= 0 || minSize > maxSize)
            {
                throw new InvalidOperationException();
            }

            Random r = new Random(0xFEED);

            BufferSegment<T> last = null;
            BufferSegment<T> first = null;
            var ownedBuffer = new OwnedArray<T>(buffer);

            int remaining = buffer.Length;
            int position = 0;
            while (remaining > 0)
            {
                int take = Math.Min(r.Next(minSize, maxSize), remaining);
                BufferSegment<T> current = new BufferSegment<T>();
                current.SetMemory(ownedBuffer, position, position + take);
                if (first == null)
                {
                    first = current;
                    last = current;
                }
                else
                {
                    last.SetNext(current);
                    last = current;
                }
                remaining -= take;
                position += take;
            }

            return new ReadOnlySequence<T>(first, 0, last, last.Length);
        }

        public static ReadOnlySequence<T> CreateBuffer<T>(params T[][] inputs) where T : struct
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException();
            }

            BufferSegment<T> last = null;
            BufferSegment<T> first = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                T[] source = inputs[i];
                int length = source.Length;
                int dataOffset = length;

                // Shift the incoming data for testing
                T[] chars = new T[length * 8];
                for (int j = 0; j < length; j++)
                {
                    chars[dataOffset + j] = source[j];
                }

                // Create a segment that has offset relative to the OwnedMemory and OwnedMemory itself has offset relative to array
                var ownedBuffer = new OwnedArray<T>(chars);
                var current = new BufferSegment<T>();
                current.SetMemory(ownedBuffer, length, length * 2);
                if (first == null)
                {
                    first = current;
                    last = current;
                }
                else
                {
                    last.SetNext(current);
                    last = current;
                }
            }

            return new ReadOnlySequence<T>(first, 0, last, last.Length);
        }

        public static ReadOnlySequence<byte> CreateUtf8Buffer(params string[] inputs)
        {
            var buffers = new byte[inputs.Length][];
            for (int i = 0; i < inputs.Length; i++)
            {
                buffers[i] = Encoding.UTF8.GetBytes(inputs[i]);
            }
            return CreateBuffer(buffers);
        }

        public static ReadOnlySequence<T> CreateBuffer<T>(params int[] inputs) where T : struct
        {
            T[][] buffers;
            if (inputs.Length == 0)
            {
                buffers = new[] { new T[] { } };
            }
            else
            {
                buffers = new T[inputs.Length][];
                for (int i = 0; i < inputs.Length; i++)
                {
                    buffers[i] = new T[inputs[i]];
                }
            }
            return CreateBuffer<T>(buffers);
        }

        public static void FillIntegerUtf8Array(byte[] array, int minValue, int maxValue, int seed = 42)
        {
            Random r = new Random(seed);

            Span<byte> span = new Span<byte>(array);

            // Generate ints across the entire range
            int next = r.Next(minValue + 1, maxValue) + r.Next(-1, 2);
            while (Utf8Formatter.TryFormat(next, span, out int written) && span.Length > written)
            {
                next = r.Next(minValue + 1, maxValue) + r.Next(-1, 2);
                span = span.Slice(written + 1);
            }
        }
    }
}
