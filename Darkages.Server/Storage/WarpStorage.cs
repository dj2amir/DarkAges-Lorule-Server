﻿///************************************************************************
//Project Lorule: A Dark Ages Client (http://darkages.creatorlink.net/index/)
//Copyright(C) 2018 TrippyInc Pty Ltd
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//*************************************************************************/

using System.IO;
using Darkages.Types;
using Newtonsoft.Json;

namespace Darkages.Storage
{
    public class WarpStorage : IStorage<WarpTemplate>
    {
        public static string StoragePath;

        static WarpStorage()
        {
            if (ServerContextBase.StoragePath == null)
                ServerContextBase.LoadConstants();

            StoragePath = $@"{ServerContextBase.StoragePath}\templates\warps";

            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public WarpTemplate Load(string Name)
        {
            var path = Path.Combine(StoragePath, $"{Name.ToLower()}.json");

            if (!File.Exists(path))
                return null;

            using (var s = File.OpenRead(path))
            using (var f = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<WarpTemplate>(f.ReadToEnd(), StorageManager.Settings);
            }
        }

        public void Save(WarpTemplate obj)
        {
            if (ServerContextBase.Paused)
                return;


            var path = Path.Combine(StoragePath, $"{obj.Name.ToLower()}.json");
            var objString = JsonConvert.SerializeObject(obj, StorageManager.Settings);
            File.WriteAllText(path, objString);
        }

        public void CacheFromStorage()
        {
            var area_dir = StoragePath;
            if (!Directory.Exists(area_dir))
                return;
            var area_names = Directory.GetFiles(area_dir, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var area in area_names)
            {
                var obj = StorageManager.WarpBucket.Load(Path.GetFileNameWithoutExtension(area));
                ServerContextBase.GlobalWarpTemplateCache.Add(obj);
            }
        }
    }
}