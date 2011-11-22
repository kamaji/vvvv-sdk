﻿
using System;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;

namespace VVVV.Hosting.Streams
{
	// Slow
	abstract class PluginConfigStream<T> : IIOStream<T>
	{
		public object Clone()
		{
			throw new NotImplementedException();
		}
		
		public abstract int Length
		{
			get;
			set;
		}
		
		protected abstract T GetSlice(int index);
		
		protected abstract void SetSlice(int index, T value);
		
		public T Read(int stride)
		{
			var result = GetSlice(ReadPosition);
			ReadPosition += stride;
			return result;
		}
		
		public int Read(T[] buffer, int index, int length, int stride)
		{
			var numSlicesToRead = StreamUtils.GetNumSlicesToRead(this, index, length, stride);
			for (int i = index; i < index + numSlicesToRead; i++)
			{
				buffer[i] = Read(stride);
			}
			return numSlicesToRead;
		}
		public void ReadCyclic(T[] buffer, int index, int length, int stride)
		{
			StreamUtils.ReadCyclic(this, buffer, index, length, stride);
		}
		
		public void Write(T value, int stride)
		{
			SetSlice(WritePosition, value);
			WritePosition += stride;
		}
		
		public int Write(T[] buffer, int index, int length, int stride)
		{
			var numSlicesToWrite = StreamUtils.GetNumSlicesToWrite(this, index, length, stride);
			for (int i = index; i < index + numSlicesToWrite; i++)
			{
				Write(buffer[i], stride);
			}
			return numSlicesToWrite;
		}
		
		public void Reset()
		{
			ReadPosition = 0;
			WritePosition = 0;
		}
		
		public int ReadPosition
		{
			get;
			set;
		}
		
		public bool Eof
		{
			get
			{
				return ReadPosition >= Length || WritePosition >= Length;
			}
		}
		
		public int WritePosition
		{
			get;
			set;
		}
		
		public abstract bool Sync();
		
		public void Flush()
		{
			// Nothing to do
		}
	}
	
	class StringConfigStream : PluginConfigStream<string>
	{
		private readonly IStringConfig FStringConfig;
		
		public StringConfigStream(IStringConfig stringConfig)
		{
			FStringConfig = stringConfig;
		}
		
		protected override string GetSlice(int index)
		{
			string result;
			FStringConfig.GetString(index, out result);
			result = result ?? string.Empty;
			return result;
		}
		
		protected override void SetSlice(int index, string value)
		{
			FStringConfig.SetString(index, value);
		}
		
		public override int Length
		{
			get
			{
				return FStringConfig.SliceCount;
			}
			set
			{
				FStringConfig.SliceCount = value;
			}
		}
		
		public override bool Sync()
		{
			return FStringConfig.PinIsChanged;
		}
	}
	
	class EnumConfigStream<T> : PluginConfigStream<T>
	{
		protected readonly IEnumConfig FEnumConfig;
		
		public EnumConfigStream(IEnumConfig enumConfig)
		{
			FEnumConfig = enumConfig;
		}
		
		protected override void SetSlice(int index, T value)
		{
			FEnumConfig.SetString(index, value.ToString());
		}
		
		public override int Length
		{
			get
			{
				return FEnumConfig.SliceCount;
			}
			set
			{
				FEnumConfig.SliceCount = value;
			}
		}
		
		protected override T GetSlice(int index)
		{
			string entry;
			FEnumConfig.GetString(index, out entry);
			try
			{
				return (T)Enum.Parse(typeof(T), entry);
			}
			catch (Exception)
			{
				return default(T);
			}
		}
		
		public override bool Sync()
		{
			return FEnumConfig.PinIsChanged;
		}
	}
	
	class DynamicEnumConfigStream : EnumConfigStream<EnumEntry>
	{
		public DynamicEnumConfigStream(IEnumConfig enumConfig)
			: base(enumConfig)
		{
		}
		
		protected override EnumEntry GetSlice(int index)
		{
			int ord;
			string name;
			FEnumConfig.GetOrd(index, out ord);
			// TODO: Was not used. FEnumName.
			FEnumConfig.GetString(index, out name);
			return new EnumEntry(name, ord);
		}
		
		protected override void SetSlice(int index, EnumEntry value)
		{
			FEnumConfig.SetOrd(index, value.Index);
		}
	}
}
