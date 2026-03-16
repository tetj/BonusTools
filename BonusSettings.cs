using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace BonusTools
{
    public class BonusSettings : ObservableObject
    {
        private string min = "70";
        public string Min { get => min; set => SetValue(ref min, value); }

        private string sleep = "400";
        public string Sleep { get => sleep; set => SetValue(ref sleep, value); }

        private string steamApiKey = "BECEC3BB7A813B2277C7BE593EBA79BD";
        public string SteamApiKey { get => steamApiKey; set => SetValue(ref steamApiKey, value); }

        private string clientID = "j8jsa14md3xge0nkdn9d0sjh56svqp";
        public string ClientID { get => clientID; set => SetValue(ref clientID, value); }

        private string clientSecret = "4j6o7t555ix3j2bm1jyscwyyvybc10";
        public string ClientSecret { get => clientSecret; set => SetValue(ref clientSecret, value); }

        private string custom = "c:\\myCustomSpreadsheet.xlsx";
        public string Custom { get => custom; set => SetValue(ref custom, value); }

        private string master = "c:\\Playstation Plus Master List.xlsx";
        public string Master { get => master; set => SetValue(ref master, value); }

        private string transac = "c:\\TransactionDetails.xlsx";
        public string Transac { get => transac; set => SetValue(ref transac, value); }

        private string nintendoRomBackup = "";
        public string NintendoRomBackup { get => nintendoRomBackup; set => SetValue(ref nintendoRomBackup, value); }

        private bool updatePlayCountFromNintendo = false;
        public bool UpdatePlayCountFromNintendo { get => updatePlayCountFromNintendo; set => SetValue(ref updatePlayCountFromNintendo, value); }

        private bool releaseYearMustMatch = true;
        public bool ReleaseYearMustMatch { get => releaseYearMustMatch; set => SetValue(ref releaseYearMustMatch, value); }

        private bool platformMustMatch = true;
        public bool PlatformMustMatch { get => platformMustMatch; set => SetValue(ref platformMustMatch, value); }

        private bool addLinkToSensCritique = true;
        public bool AddLinkToSensCritique { get => addLinkToSensCritique; set => SetValue(ref addLinkToSensCritique, value); }

        private bool updatePlayCountFromSteam = true;
        public bool UpdatePlayCountFromSteam { get => updatePlayCountFromSteam; set => SetValue(ref updatePlayCountFromSteam, value); }

        private bool updatePlayCountFromSensCritique = true;
        public bool UpdatePlayCountFromSensCritique { get => updatePlayCountFromSensCritique; set => SetValue(ref updatePlayCountFromSensCritique, value); }

        private bool updateUserScoreFromSensCritique = true;
        public bool UpdateUserScoreFromSensCritique { get => updateUserScoreFromSensCritique; set => SetValue(ref updateUserScoreFromSensCritique, value); }

        private bool updateCommunityScore = true;
        public bool UpdateCommunityScore { get => updateCommunityScore; set => SetValue(ref updateCommunityScore, value); }

        private double int1 = 300;
        public double Int1
        {
            get => int1;
            set
            {
                int1 = value;
                OnPropertyChanged();
            }
        }

    }
    public class BonusSettingsViewModel : ObservableObject, ISettings
    {
        private BonusTools plugin;

        private BonusSettings editingClone { get; set; }

        private BonusSettings settings;
        public BonusSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public BonusSettingsViewModel(BonusTools plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // The old EndEdit bug saved the ViewModel (wrapped as {"Settings":{...}}) instead of
            // Settings directly. Try loading in that old wrapper format first so the saved values
            // are not lost. Once the user saves again, EndEdit writes the correct flat format and
            // this branch is no longer taken.
            var wrapper = plugin.LoadPluginSettings<LegacySettingsWrapper>();
            if (wrapper?.Settings != null)
            {
                Settings = wrapper.Settings;
            }
            else
            {
                Settings = plugin.LoadPluginSettings<BonusSettings>() ?? new BonusSettings();
            }
        }

        private class LegacySettingsWrapper
        {
            public BonusSettings Settings { get; set; }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        // To save settings just call SavePluginSettings when user confirms changes.
        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}
