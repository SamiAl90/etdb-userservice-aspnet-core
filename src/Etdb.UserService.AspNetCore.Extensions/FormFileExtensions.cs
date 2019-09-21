﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Etdb.UserService.AspNetCore.Extensions
{
    public static class FormFileExtensions
    {
        public static async Task<byte[]> ReadFileBytesAsync(this IFormFile file)
        {
            var totalSize = file.Length;
            var fileBytes = new byte[file.Length];

            await using (var fileStream = file.OpenReadStream())
            {
                var offset = 0;

                while (offset < file.Length)
                {
                    var chunkSize = totalSize - offset < 4096L ? (int) totalSize - offset : 4096;

                    offset += await fileStream.ReadAsync(fileBytes, offset, chunkSize);
                }
            }

            return fileBytes;
        }
    }
}