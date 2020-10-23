using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ns
{
    public class IncrementableLockFile
    {
        private const int LongSize = 20;
        private const int Retries = 100;
        private readonly string _filePath;
        private readonly long _maxValue;
        private long _index = (long)LockStates.NotInitialized;

        public IncrementableLockFile(string filePath, long maxValue)
        {
            _filePath = filePath;
            _maxValue = maxValue;
        }

        public async Task<long> GetNextIndex()
        {
            for (var iteration = 0; iteration < Retries; iteration++)
            {
                try
                {
                    await using var fileStream = _index == (long)LockStates.NotInitialized
                        ? File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                        : File.Open(_filePath, FileMode.Open, FileAccess.ReadWrite);
                    if (fileStream.Length == 0)
                    {
                        return await HandleEmptyFile(fileStream);
                    }

                    return await HandleIndexValue(fileStream);
                }
                catch (FileNotFoundException)
                {
                    _index = (long)LockStates.FileRemoved;
                    return _index;
                }
                catch (Exception)
                {
                    await Task.Delay(100);
                }
            }

            throw new IOException($"Unable to access file '{_filePath}' after '{Retries}' retries.");
        }

        public void RemoveIndexFile()
        {
            File.Delete(_filePath);
            _index = (long)LockStates.FileRemoved;
        }

        private async Task<long> HandleEmptyFile(Stream fileStream)
        {
            _index = 0;
            await fileStream.WriteAsync(Encoding.UTF8.GetBytes("1"));
            return _index;
        }

        private async Task<long> HandleIndexValue(Stream fileStream)
        {
            _index = await GetIndexValue(fileStream);
            if (_index < 0)
            {
                return _index;
            }
            if (_index > _maxValue)
            {
                _index = (long)LockStates.OverMaxValue;
                return _index;
            }

            fileStream.Position = 0;
            await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"{_index + 1}"));

            return _index;
        }

        private static async Task<long> GetIndexValue(Stream fileStream)
        {
            var buffer = new byte[LongSize];
            await fileStream.ReadAsync(buffer);
            return Convert.ToInt64(Encoding.UTF8.GetString(buffer));
        }
    }

    public enum LockStates
    {
        NotInitialized = -1,
        OverMaxValue = -2,
        FileRemoved = -3,
    }
}
