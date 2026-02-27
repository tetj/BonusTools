using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace BonusTools
{
    public class BonusSettings : ObservableObject
    {
        [DontSerialize]
        private string min = "70";
        public string Min { get => min; set => SetValue(ref min, value); }

        [DontSerialize]
        private string sleep = "400";
        public string Sleep { get => sleep; set => SetValue(ref sleep, value); }

        [DontSerialize]
        private string steamApiKey = "BECEC3BB7A813B2277C7BE593EBA79BD";
        public string SteamApiKey { get => steamApiKey; set => SetValue(ref steamApiKey, value); }

        [DontSerialize]
        private string clientID = "j8jsa14md3xge0nkdn9d0sjh56svqp";
        public string ClientID { get => clientID; set => SetValue(ref clientID, value); }

        [DontSerialize]
        private string clientSecret = "4j6o7t555ix3j2bm1jyscwyyvybc10";
        public string ClientSecret { get => clientSecret; set => SetValue(ref clientSecret, value); }

        [DontSerialize]
        private string custom = "c:\\myCustomSpreadsheet.xlsx";
        public string Custom { get => custom; set => SetValue(ref custom, value); }

        [DontSerialize]
        private string master = "c:\\Playstation Plus Master List.xlsx";
        public string Master { get => master; set => SetValue(ref master, value); }

        [DontSerialize]
        private string transac = "c:\\TransactionDetails.xlsx";
        public string Transac { get => transac; set => SetValue(ref transac, value); }

        [DontSerialize]
        private string nintendoRomBackup = "";
        public string NintendoRomBackup { get => nintendoRomBackup; set => SetValue(ref nintendoRomBackup, value); }

        [DontSerialize]
        private bool updatePlayCountFromNintendo = false;
        public bool UpdatePlayCountFromNintendo { get => updatePlayCountFromNintendo; set => SetValue(ref updatePlayCountFromNintendo, value); }

        [DontSerialize]
        private bool releaseYearMustMatch = true;
        public bool ReleaseYearMustMatch { get => releaseYearMustMatch; set => SetValue(ref releaseYearMustMatch, value); }

        [DontSerialize]
        private bool platformMustMatch = true;
        public bool PlatformMustMatch { get => platformMustMatch; set => SetValue(ref platformMustMatch, value); }

        [DontSerialize]
        private bool addLinkToSensCritique = true;
        public bool AddLinkToSensCritique { get => addLinkToSensCritique; set => SetValue(ref addLinkToSensCritique, value); }

        [DontSerialize]
        private bool updatePlayCountFromSteam = true;
        public bool UpdatePlayCountFromSteam { get => updatePlayCountFromSteam; set => SetValue(ref updatePlayCountFromSteam, value); }

        [DontSerialize]
        private bool updatePlayCountFromSensCritique = true;
        public bool UpdatePlayCountFromSensCritique { get => updatePlayCountFromSensCritique; set => SetValue(ref updatePlayCountFromSensCritique, value); }

        [DontSerialize]
        private bool updateUserScoreFromSensCritique = true;
        public bool UpdateUserScoreFromSensCritique { get => updateUserScoreFromSensCritique; set => SetValue(ref updateUserScoreFromSensCritique, value); }

        [DontSerialize]
        private bool updateCommunityScore = true;
        public bool UpdateCommunityScore { get => updateCommunityScore; set => SetValue(ref updateCommunityScore, value); }


        [DontSerialize]
        private double int1 { get; set; } = 300;
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

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<BonusSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new BonusSettings();
            }
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
            plugin.SavePluginSettings(this);
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
