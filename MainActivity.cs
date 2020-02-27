using System;
using System.Text;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Nfc;
using Android.Nfc.Tech;
namespace Smartisan.Nfc
{
    [Activity(Label = "NFC_APP", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity//, Android.Nfc.NfcAdapter.ICreateNdefMessageCallback
    {
        // déclaration variables
        NfcAdapter nfcAdapter;
        PendingIntent nfcPi;
        IntentFilter nfcFilter;
        Tag nfcTag;
        string newLine = System.Environment.NewLine;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // afficher l'unique page
            SetContentView(Resource.Layout.Main);

            // Chercher et connecter les boutons à leur fonction
            Button ScanButton = FindViewById<Button>(Resource.Id.ScanButton); // bouton scan
            ScanButton.Click += Scan;

            // button RAZ
            Button Raz_button = FindViewById<Button>(Resource.Id.RAZ);
            Raz_button.Click += OnClickRAZ; ;

            var writerButton = FindViewById<Button>(Resource.Id.WriteButton);
            writerButton.Click += Write;
            // pour la detection de la puce
            var label = FindViewById<TextView>(Resource.Id.Information);

            // information pour l'écriture de l'information
            var Info = FindViewById<TextView>(Resource.Id.Information);
            Info.Text = "";

            nfcAdapter = NfcAdapter.GetDefaultAdapter(ApplicationContext);
            if (nfcAdapter == null) // détection tag NFC
            {
                label.Text = "Aucune détection de puce NFC";
                return;
            }

            if (!nfcAdapter.IsEnabled) // si l'option NFC est désactivée
            {
                label.Text = "NFC désactivé\n Veuillez activer le NFC ";
                return;
            }

            //nfcAdapter.SetNdefPushMessageCallback(this, this);
            var intent = new Intent(this, this.Class);
            intent.AddFlags(ActivityFlags.SingleTop);
            nfcPi = PendingIntent.GetActivity(this, 0, intent, 0);
            nfcFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
            nfcFilter.AddCategory(Intent.CategoryDefault);
        }

        protected override void OnResume()
        {
            base.OnResume();

            lock (this)
            {
                nfcAdapter.EnableForegroundDispatch(this, nfcPi, new IntentFilter[] { nfcFilter }, null);
            }


            if (NfcAdapter.ActionTechDiscovered == Intent.Action)
            {
                ProcessIntent(Intent);
            }
            else
            {
                return;
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            if (NfcAdapter.ActionNdefDiscovered == intent.Action)
            {
                ProcessIntent(Intent);
            }
            else
            {
                return;
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            lock (this)
            {
                nfcAdapter.DisableForegroundDispatch(this);
                var label = FindViewById<TextView>(Resource.Id.DataLabel);
                label.Text = string.Empty;

            }

        }

        private void Scan(object sender, EventArgs e) // scaner la puce
        {
            // information pour l'écriture de l'information
            var Info = FindViewById<TextView>(Resource.Id.Information);
            Info.Text = "";
            this.Scan(); // appel de la procédure scan
        }

        private void Scan() // scaner la puce
        {
            var nom = FindViewById<TextView>(Resource.Id.Cont_nom); // champ rempli par l'utilisateur
            var mdp = FindViewById<TextView>(Resource.Id.Cont_MDP); // champs rempli par l'utilisateur
            var textnom = FindViewById<TextView>(Resource.Id.textLogin); // champ rempli par l'utilisateur
            var textmdp = FindViewById<TextView>(Resource.Id.textMDP); // champs rempli par l'utilisateur
            var Info = FindViewById<TextView>(Resource.Id.Information);
            lock (this) // vérou d'exclusion de l'objet this
            {
                var label = FindViewById<TextView>(Resource.Id.DataLabel);
                textmdp.Text = null;
                textnom.Text = null;
                Info.Text = null;
                try
                {
                    if (nfcTag == null) // vérifier le contenu de la puce
                    {
                        Info.Text = "La puce NFC est vide ou detectable";
                       
                    } else if (textnom.Text == null & textmdp.Text == null)
                    {
                        Info.Text = "Puce vide";
                    }

                    var ndef = Ndef.Get(nfcTag);

                    ndef.Connect();
                    var data = Encoding.UTF8.GetString(ndef.NdefMessage.ToByteArray()); // récupération données
                    Console.WriteLine(data);
                    ndef.Close();

                    //label.Text = data; // affiche le contenu
                    char[] splitters = new char[] { '/' }; // splitter pour séparer login et mdp
                    char[] splittersLogin = new char[] { '=' };  // splitter  pour récuperer le login
                    string[] contenu = data.Split(splitters); // contrenu[0] = login, contenu[1] = mdp
                    string[] contenuLogin = contenu[0].Split(splittersLogin); // récuperer le contenu du login
                    textnom.Text = contenuLogin[1]; // contenuLogin[1] vaut la valeur du login
                    char[] splitterMDP = new char[] { ':' }; //splitter pour récuperer le mdp
                    string[] contenuMDP = contenu[1].Split(splitterMDP); // récuperer le contenu de mdp
                    textmdp.Text = contenuMDP[1]; // contenu[1] vaut la valeur du mdp
                }
                catch (Exception) // gestion exception
                {
                    if (textnom.Text == "" & textmdp.Text == "") // si la puce est détectée mais est vide
                    {
                        Info.Text = " Puce vide"; // information
                    }
                    else if (nfcTag == null) // si la puce n'est pas détectée
                    {
                        Info.Text = "La puce NFC est indétectable"; // information
                    }
                }
                finally
                {
                    if (nfcTag != null)
                    {
                        if (Ndef.Get(nfcTag).IsConnected)
                        {
                            Ndef.Get(nfcTag).Close();
                        }
                    }
                }
            }

        }

        private void Write(object sender, EventArgs e) // écriture de la puce
        {

            lock (this) // verou sur l'objet this
            {
                var label = FindViewById<TextView>(Resource.Id.DataLabel);
                // variables pour récuperer saisie de l'utilisateur
                EditText nomET = FindViewById<EditText>(Resource.Id.nomET); // champ rempli par l'utilisateur
                EditText mdpET = FindViewById<EditText>(Resource.Id.mdpET); // champs rempli par l'utilisateur
                string nom = nomET.Text; // nom = valeur du champs rempli par l'utilisateur
                string mdp = mdpET.Text;// mdp = valeur du champs rempli par l'utilisateur

                // information pour l'écriture de l'information
                var Info = FindViewById<TextView>(Resource.Id.Information);
                Info.Text = "";

                try
                {
                    if (nfcTag == null) // si la puce est vide
                    {
                        label.Text = "La puce NFC est vide ou indétectable";
                        return;
                    }
                    // si nomET et mdpET renseignés alors écriture sur la puce sinon message d'erreur
                    if (nom != "" & mdp != "")
                    {
                        string data = "\nLogin= " + nomET.Text + "/" + "MDP: " + mdpET.Text;
                        // var data = nomET.Text + "/" + mdpET.Text;
                        
                        var ndefRecord = new NdefRecord(NdefRecord.TnfMimeMedia,
                            null,
                            new byte[] { },
                            Encoding.UTF8.GetBytes(data)); // payload
                        Info.Text = "ndef " +ndefRecord;
                        Console.WriteLine(data);
                        var ndef = Ndef.Get(nfcTag);
                        ndef.Connect();
                        ndef.WriteNdefMessage(new NdefMessage(ndefRecord));
                        ndef.Close();
                        Info.Text = "Puce écrite";

                        // remise à zéro des champs de saisie
                        nomET.Text = string.Empty;
                        mdpET.Text = string.Empty;
                        // mise à jour des informations contenues dans la puce en appellant la procédure Scann
                        //Scan la puce qui vient d'être écrite
                        this.Scan();
                    }
                    else
                    {
                        Info.Text = "Veillez saisir le login et le mot de passe\nEcriture interrompue";
                    }
                }

                catch (Exception) // gestion d'une eventuelle exeption pour éviter que l'appli plante
                {
                    Info.Text = "Puce indétectable";
                }
                finally
                {
                    if (nfcTag != null)
                    {
                        if (Ndef.Get(nfcTag).IsConnected)
                        {
                            Ndef.Get(nfcTag).Close();
                        }
                    }
                }
            }
        }

        private void ProcessIntent(Intent intent)
        {
            var label = FindViewById<TextView>(Resource.Id.Information);

            try
            {
                lock (this)
                {
                    nfcTag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                }
                this.Scan(); // appel de scan
            }
            catch (Exception ex)// gestion d'une exception pour éviter que l'appli plante
            {
                label.Text += $"{newLine} Exception: {newLine} {ex.Message} {newLine} {ex.StackTrace}";
            }
            finally
            {
                if (Ndef.Get(nfcTag).IsConnected)
                {
                    Ndef.Get(nfcTag).Close();
                }
            }
        }
        private void OnClickRAZ(object sender, EventArgs args)
        {
            EditText nomET = FindViewById<EditText>(Resource.Id.nomET); // champ rempli par l'utilisateur
            EditText mdpET = FindViewById<EditText>(Resource.Id.mdpET); // champs rempli par l'utilisateur
            var Info = FindViewById<TextView>(Resource.Id.Information); // information pour l'écriture de l'information
            var nom = FindViewById<TextView>(Resource.Id.textLogin); // champ rempli par l'utilisateur
            var mdp = FindViewById<TextView>(Resource.Id.textMDP); // champs rempli par l'utilisateur
            nom.Text = ""; //remise à zéro de la lecture de la puce NFC
            mdp.Text = ""; //remise à zéro de la lecture de la puce NFC
            Info.Text = ""; //remise à zéro du champ info
            nomET.Text = string.Empty; // remise à zéro des champs saisis
            mdpET.Text = string.Empty; // remise à zéro des champs saisis
        }
    }
}