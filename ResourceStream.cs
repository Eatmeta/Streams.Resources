using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Streams.Resources
{
    public class ResourceReaderStream : Stream
    {
        private Stream Stream { get; }
        private string Key { get; set; }
        private List<byte> LastKeyBuffer { get; set; } = new List<byte>();
        private byte[] EncodedKey => Encoding.ASCII.GetBytes(Key);
        private byte[] Buffer { get; } = new byte[Constants.BufferSize];
        private int LastKeyStartIndex { get; set; }
        private int LastKeyEndIndex { get; set; }
        private bool IsKeyStartIndexFound { get; set; } = true;
        private int Pointer { get; set; }
        private bool IsValueEndIndexFound { get; set; }
        private bool IsValueStartIndexFound { get; set; }

        public ResourceReaderStream(Stream stream, string key)
        {
            Stream = stream;
            AddSeparationZeroToKey(key);
            if (Key == null)
                Key = key;
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (IsValueEndIndexFound) return 0;
            if (IsUpdateBufferRequired())
            {
                var read = UpdateBuffer(offset);
                if (read == 0) return 0;
            }
            var countOfBytesInBuffer = Math.Min(count, Constants.BufferSize - Pointer);

            while (!IsValueStartIndexFound)
            {
                TryToFindValueStartIndex(offset, ReturnCountOfBytesInBuffer(count));
                if (!IsUpdateBufferRequired()) continue;
                SaveTheBeginningOfLongKeyBeforeUpdateBuffer();
                UpdateBuffer(offset);
                LastKeyStartIndex = 0;
            }

            return IsValueHasNoElements() ? 0 : ReturnCountOfReadedBytes(buffer, countOfBytesInBuffer);
        }

        private int ReturnCountOfReadedBytes(byte[] buffer, int countOfBytesInBuffer)
        {
            var lengthАdjustmentBecauseZeroSeparator = 0;
            for (var i = 0; i < countOfBytesInBuffer - 1; i++) 
            {
                if (IsZeroSeparatorRequired(i))
                {
                    lengthАdjustmentBecauseZeroSeparator = -1;
                    continue;
                }
                buffer[i + lengthАdjustmentBecauseZeroSeparator] = Buffer[Pointer + i];

                if (IsSeparatorFound(i))
                {
                    IsValueEndIndexFound = true;
                    return i + 1 + lengthАdjustmentBecauseZeroSeparator;
                }
                AddTheVeryLastByteToBuffer(buffer, i, countOfBytesInBuffer);
            }
            Pointer += countOfBytesInBuffer;
            return countOfBytesInBuffer;
        }

        private void TryToFindValueStartIndex(int offset, int count)
        {
            for (var i = offset; i < offset + count - 1; i++) 
            {
                if (!IsSeparatorFound(i)) continue;

                if (IsKeyStartIndexFound)
                {
                    LastKeyEndIndex = i + Pointer;
                    if (AreKeysLengthsMatch())
                    {
                        if (AreKeysMatch())
                        {
                            Pointer += i + 3;
                            IsValueStartIndexFound = true;
                            return;
                        }
                    }
                    LastKeyBuffer = new List<byte>();
                    IsKeyStartIndexFound = false;
                }
                else
                {
                    LastKeyStartIndex = i + 3 + Pointer;
                    IsKeyStartIndexFound = true;
                }
            }
            Pointer += count;
        }
        
        private void AddTheVeryLastByteToBuffer(byte[] buffer, int i, int countOfBytesInBuffer)
        {
            if (i == countOfBytesInBuffer - 2)
                buffer[countOfBytesInBuffer - 1] = Buffer[Pointer + countOfBytesInBuffer - 1];
        }

        private void SaveTheBeginningOfLongKeyBeforeUpdateBuffer()
        {
            if (!IsKeyStartIndexFound) return;
            for (var i = LastKeyStartIndex; i < Buffer.Length; i++)
                LastKeyBuffer.Add(Buffer[i]);
        }

        private int ReturnCountOfBytesInBuffer(int count) => Math.Min(count, Constants.BufferSize - Pointer);

        private bool IsZeroSeparatorRequired(int i) => Buffer[Pointer + i] == 0 && Buffer[Pointer + i + 1] == 0;

        private bool IsUpdateBufferRequired() => Pointer % 1024 == 0;

        private bool IsSeparatorFound(int i)
            => Buffer[Pointer + i] != 0 && Buffer[Pointer + i + 1] == 0 && Buffer[Pointer + i + 2] == 1;

        private bool IsValueHasNoElements() => Buffer[Pointer] == 0 && Buffer[Pointer + 1] == 1;

        private bool AreKeysLengthsMatch() =>
            LastKeyEndIndex - LastKeyStartIndex + LastKeyBuffer.Count == EncodedKey.Length - 1;

        private bool AreKeysMatch()
        {
            var exportBuffer = Buffer.Skip(LastKeyStartIndex).Take(LastKeyEndIndex - LastKeyStartIndex + 1).ToList();
            LastKeyBuffer.AddRange(exportBuffer);
            return EncodedKey.SequenceEqual(LastKeyBuffer);
        }

        private int UpdateBuffer(int offset)
        {
            var read = Stream.Read(Buffer, offset, Constants.BufferSize);
            Pointer = 0;
            return read == 0
                ? throw new EndOfStreamException("Key not found!")
                : read;
        }

        private void AddSeparationZeroToKey(string key)
        {
            var keyAsArray = Encoding.ASCII.GetBytes(key);
            var list = new List<byte>();
            for (var i = 0; i < keyAsArray.Length - 1; i++)
            {
                if (keyAsArray[i] != 0) continue;
                list.AddRange(keyAsArray.Take(i));
                list.Add(0);
                list.AddRange(keyAsArray.Skip(i));
                Key = Encoding.ASCII.GetString(list.ToArray());
                break;
            }
        }

        public override void Flush() { }

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}