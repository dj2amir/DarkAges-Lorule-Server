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

using System;
using System.Collections.Generic;
using System.Linq;
using Darkages.Network.Game;

namespace Darkages.Types
{
    public class Party
    {
        public Party(Aisling User)
        {
            Creator = User;
        }

        public string Name { get; set; }
        public Aisling Creator { get; set; }

        public List<Aisling> Members { get; set; }

        public List<Aisling> MembersExcludingSelf => Members.FindAll(i => i.Serial != Creator.Serial);

        public List<Aisling> MembersExcludingSelfNearby =>
            Members.FindAll(i => i.Serial != Creator.Serial && Creator.WithinRangeOf(i));

        public List<Aisling> MembersExcludingSelfMapWide =>
            Members.FindAll(i => i.Serial != Creator.Serial && i.CurrentMapId == Creator.CurrentMapId);

        public int Length => Members.Count;

        public int LengthExcludingSelf => Members.Count(i => i.Serial != Creator?.Serial);

        public void Create()
        {
            Members = new List<Aisling>();
            Members.Add(Creator);
        }

        public static void DisbandParty(Party prmParty)
        {
            List<Aisling> tmpCopy;
            tmpCopy = new List<Aisling>(prmParty.Members);

            foreach (var member in tmpCopy)
            {
                RemoveFromParty(prmParty, member, true);
                RemoveFromParty(member.GroupParty, prmParty.Creator, true);
            }
        }

        public static bool AddToParty(Party prmParty, Aisling User)
        {
            if (prmParty.Members.Find(i =>
                string.Equals(i.Username, User.Username,
                    StringComparison.OrdinalIgnoreCase)) == null)
            {
                User.InvitePrivleges = true;
                prmParty.Members.Add(User);
                return true;
            }

            return false;
        }

        public static bool RemoveFromParty(Party prmParty, Aisling User, bool disbanded = false)
        {
            if (User.Username.Equals(prmParty.Creator.Username, StringComparison.OrdinalIgnoreCase))
                return false;

            var idx = prmParty.Members.FindIndex(i =>
                string.Equals(i.Username, User.Username,
                    StringComparison.OrdinalIgnoreCase));

            if (idx < 0)
                return false;

            User.LeaderPrivleges = false;
            User.InvitePrivleges = true;

            prmParty.Members.RemoveAt(idx);

            prmParty.Creator.Client.SendMessage(0x02,
                !disbanded ? $"{User.Username} has left the party." : "Party Disbanded.");
            User.Client.SendMessage(0x02,
                !disbanded ? $"{prmParty.Creator.Username} has left the party." : "Party Disbanded.");


            if (!disbanded && prmParty.LengthExcludingSelf == 0)
            {
                prmParty.Creator.LeaderPrivleges = false;
                prmParty.Creator.InvitePrivleges = true;
                User.LeaderPrivleges = false;
                User.InvitePrivleges = true;

                Reform(User.Client);
                Reform(prmParty.Creator.Client);
            }

            return true;
        }


        public bool RequestUserToJoin(Aisling userRequested)
        {
            if (Creator == null)
                return false;

            if (userRequested.Username.Equals(Creator.Username,
                StringComparison.OrdinalIgnoreCase))
                return false;

            if (userRequested.LeaderPrivleges)
            {
                Creator.Client.SendMessage(0x02,
                    "Only a party leader can do that.");
                return false;
            }

            if (Creator.GroupParty.MembersExcludingSelf.Find(i
                => string.Equals(i.Username, userRequested.Username,
                    StringComparison.OrdinalIgnoreCase)) != null)
            {
                RemoveFromParty(Creator.GroupParty, userRequested);
                RemoveFromParty(userRequested.GroupParty, Creator);

                if (Creator.GroupParty.LengthExcludingSelf == 0)
                    Creator.Client.SendMessage(0x02,
                        "Your party has been disbanded.");

                if (userRequested.GroupParty.LengthExcludingSelf == 0)
                    userRequested.Client.SendMessageBox(0x02,
                        "Your party has been disbanded.");
            }
            else
            {
                if (!Creator.InvitePrivleges)
                    return false;

                //Check is this user is already in a party.
                if (userRequested.GroupParty.LengthExcludingSelf > 0)
                {
                    Creator.Client.SendMessage(0x02,
                        $"{userRequested.Username} Is a member of another party.");

                    return false;
                }

                var a = AddToParty(userRequested.GroupParty, Creator);
                userRequested.Client.SendMessage(0x02,
                    $"{Creator.Username} has joined your party.");

                var b = AddToParty(Creator.GroupParty, userRequested);
                Creator.Client.SendMessage(0x02,
                    $"{userRequested.Username} has joined your party.");

                return a && b;
            }

            return false;
        }

        public static void WithDrawFromParty(GameClient client)
        {
            if (client == null || client.Aisling == null)
                return;

            foreach (var m in client.Aisling.GroupParty.MembersExcludingSelf)
                RemoveFromParty(m.GroupParty, client.Aisling);

            RemoveFromParty(client.Aisling.GroupParty, client.Aisling);
        }

        public static void Reform(GameClient client)
        {
            if (client == null || client.Aisling == null)
                return;

            client.Aisling.GroupParty = new Party(client.Aisling);
            client.Aisling.GroupParty.Create();
        }

        public bool Has(Aisling aisling, bool includeSelf = false)
        {
            if (includeSelf)
                return Members.Any(i => i.Serial == aisling.Serial);

            return MembersExcludingSelf.Any(i => i.Serial == aisling.Serial);
        }
    }
}