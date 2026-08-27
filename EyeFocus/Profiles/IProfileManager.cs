using System;
using System.Collections.Generic;
using EyeFocus.Models;

namespace EyeFocus.Profiles
{
    public interface IProfileManager
    {
        IReadOnlyList<DisplayProfile> GetAllProfiles();
        DisplayProfile? GetProfile(string id);
        DisplayProfile GetActiveProfile();
        void SetActiveProfile(string id);
        
        DisplayProfile SaveProfile(DisplayProfile profile);
        DisplayProfile SaveAsNew(DisplayProfile profile, string newName);
        DisplayProfile ResetProfileToDefault(string id);
        bool DeleteProfile(string id);
        DisplayProfile DuplicateProfile(string id, string? newName = null);

        event EventHandler<DisplayProfile>? ActiveProfileChanged;
        event EventHandler? ProfilesListChanged;
    }
}
