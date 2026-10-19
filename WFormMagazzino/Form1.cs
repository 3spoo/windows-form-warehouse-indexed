
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static WFormMagazzino.Form1;

namespace WFormMagazzino
{
    public partial class Form1 : Form
    {
        public List<Indice> indici = new List<Indice>();

        const string FILE_ARCHIVIO = "ARCHIVIO.dat";
        const string FILE_INDICI = "INDICI.txt";

        const int DIM_NOME = 15;
        const int DIM_RECORD = 4 + DIM_NOME * 2 + 16 + 4 + 1;

        static int ID_COUNTER;

        public Form1()
        {
            InitializeComponent();
            GeneraFile();
            AggiornaDGV();
        }

        // ** FUNZIONI **
        public void GeneraFile()
        {
            using (FileStream fs = new FileStream(FILE_INDICI, FileMode.OpenOrCreate))
            {
            }

            using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.OpenOrCreate))
            {
            }

            indici.Clear();

            foreach (string riga in File.ReadAllLines(FILE_INDICI))
            {
                if (string.IsNullOrWhiteSpace(riga))
                    continue;

                string[] dati = riga.Split(';');

                if (dati.Length != 2)
                    continue;

                if (!int.TryParse(dati[0], out int codice) ||
                    !int.TryParse(dati[1], out int posizione))
                    continue;

                if (posizione < 0 || posizione % DIM_RECORD != 0)
                    continue;

                Indice i = new Indice(codice, posizione);
                indici.Add(i);
            }

            // ORDINA GLI INDICI PER CODICE CRESCENTE
            indici.Sort((a, b) => a.Codice.CompareTo(b.Codice));

            // ASSEGNA IL CODICE PIÙ ALTO A ID_COUNTER
            ID_COUNTER = 0;

            foreach (Indice indice in indici)
            {
                if (indice.Codice > ID_COUNTER)
                    ID_COUNTER = indice.Codice;
            }
        }

        public void SalvaIndici()
        {
            using (StreamWriter sw = new StreamWriter(FILE_INDICI, false))
            {
                foreach (Indice indice in indici)
                {
                    sw.WriteLine($"{indice.Codice};{indice.Posizione}");
                }
            }
        }

        public void AggiornaDGV()
        {
            dataGridView1.Rows.Clear();

            foreach (Indice indice in indici)
            {
                Prodotto p = LeggiProdotto(indice.Posizione);

                if (p != null && p.IsActive)
                {
                    dataGridView1.Rows.Add(
                        p.Codice,
                        p.Nome,
                        p.Prezzo,
                        p.Quantita
                    );
                }
            }
        }

        public Prodotto LeggiProdotto(int posizione)
        {
            using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open, FileAccess.Read))
            {
                if (posizione < 0 ||
                    posizione + DIM_RECORD > fs.Length)
                    throw new InvalidDataException("POSIZIONE NON VALIDA.");

                fs.Seek(posizione, SeekOrigin.Begin);

                using (BinaryReader br = new BinaryReader(fs))
                {
                    int codice = br.ReadInt32();

                    byte[] nomeBytes = br.ReadBytes(DIM_NOME * 2);

                    if (nomeBytes.Length != DIM_NOME * 2)
                        throw new EndOfStreamException();

                    string nome = Encoding.Unicode
                        .GetString(nomeBytes)
                        .TrimEnd('\0');

                    decimal prezzo = br.ReadDecimal();
                    int quantita = br.ReadInt32();
                    bool isActive = br.ReadBoolean();

                    return new Prodotto(codice, nome, prezzo, quantita, isActive);
                }
            }
        }

        public void ScriviProdotto(FileStream fs, Prodotto p, int posizione)
        {
            fs.Seek(posizione, SeekOrigin.Begin);

            using (BinaryWriter bw = new BinaryWriter(fs, Encoding.Unicode, true))
            {
                bw.Write(p.Codice);

                // NOME LENGTH_FISSA
                byte[] nomeBytes = new byte[DIM_NOME * 2];
                byte[] nomeCodificato = Encoding.Unicode.GetBytes(p.Nome);

                if (nomeCodificato.Length > nomeBytes.Length)
                    throw new ArgumentException("NOME TROPPO LUNGO.");

                Array.Copy(nomeCodificato, nomeBytes, nomeCodificato.Length);
                bw.Write(nomeBytes);

                bw.Write(p.Prezzo);
                bw.Write(p.Quantita);
                bw.Write(p.IsActive);
            }
        }

        public int BinarySearch(List<Indice> indici, int codiceDaCercare)
        {
            int sinistra = 0;
            int destra = indici.Count - 1;

            while (sinistra <= destra)
            {
                int centro = sinistra + (destra - sinistra) / 2;

                if (indici[centro].Codice == codiceDaCercare)
                {
                    return indici[centro].Posizione;
                }
                else if (indici[centro].Codice < codiceDaCercare)
                {
                    sinistra = centro + 1;
                }
                else
                {
                    destra = centro - 1;
                }
            }

            return -1; // NOT FOUND
        }

        public Prodotto CercaProdottoAttivo(int codice)
        {
            int posizione = BinarySearch(indici, codice);

            if (posizione == -1)
                return null;

            Prodotto p = LeggiProdotto(posizione);

            if (!p.IsActive)
                return null;

            return p;
        }

        // ** CLASSI **
        public class Prodotto
        {
            private int _codice;

            public int Codice
            {
                get { return _codice; }
            }

            private string _nome;

            public string Nome
            {
                get { return _nome; }
                set { _nome = value; }
            }

            private decimal _prezzo;

            public decimal Prezzo
            {
                get { return _prezzo; }
                set { _prezzo = value; }
            }

            private int _quantita;

            public int Quantita
            {
                get { return _quantita; }
                set { _quantita = value; }
            }

            private bool _isActive;

            public bool IsActive
            {
                get { return _isActive; }
                set { _isActive = value; }
            }

            public Prodotto(int c, string n, decimal p, int q, bool i)
            {
                _codice = c;
                _nome = n;
                _prezzo = p;
                _quantita = q;
                _isActive = i;
            }

            public Prodotto(string n, decimal p, int q)
            {
                _codice = ++ID_COUNTER;
                _nome = n;
                _prezzo = p;
                _quantita = q;
                _isActive = true;
            }

            public Prodotto()
            {
                _codice = 0;
                _nome = string.Empty;
                _prezzo = 0;
                _quantita = 0;
                _isActive = false;
            }
        }

        public class Indice
        {
            private int _codice;

            public int Codice
            {
                get { return _codice; }
                set { _codice = value; }
            }

            private int _posizione;

            public int Posizione
            {
                get { return _posizione; }
                set { _posizione = value; }
            }

            public Indice(int c, int p)
            {
                _codice = c;
                _posizione = p;
            }
        }

        // ** BUTTONS **
        private void btnInserisci_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("INSERISCI IL NOME.");
                return;
            }

            if (nome.Length > DIM_NOME)
            {
                MessageBox.Show($"IL NOME PUÒ CONTENERE MASSIMO {DIM_NOME} CARATTERI.");
                return;
            }

            decimal prezzo = nudPrezzo.Value;
            int quantita = (int)nudQty.Value;

            // CREA PRODOTTO
            Prodotto p = new Prodotto(nome, prezzo, quantita);

            int posizione;

            using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open, FileAccess.ReadWrite))
            {
                posizione = (int)fs.Length;

                ScriviProdotto(fs, p, posizione);
            }

            // SCRIVI INDICE
            Indice i = new Indice(p.Codice, posizione);
            indici.Add(i);

            // ORDINA
            indici.Sort((a, b) => a.Codice.CompareTo(b.Codice));

            // RISCRIVE IL FILE
            SalvaIndici();

            // AGGIORNA DGV
            dataGridView1.Rows.Add(
                p.Codice,
                p.Nome,
                p.Prezzo,
                p.Quantita
            );

            // PULISCI CAMPI
            txtNome.Clear();
            nudPrezzo.Value = nudPrezzo.Minimum;
            nudQty.Value = nudQty.Minimum;

            MessageBox.Show("PRODOTTO INSERITO.");
        }

        private void btnCerca_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtCodice.Text, out int codice))
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }

            try
            {
                int posizione = BinarySearch(indici, codice);

                if (posizione == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizione);

                if (!p.IsActive)
                {
                    MessageBox.Show("PRODOTTO ELIMINATO.");
                    return;
                }

                txtCodice.Text = p.Codice.ToString();
                txtNome.Text = p.Nome;
                nudPrezzo.Value = p.Prezzo;
                nudQty.Value = p.Quantita;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERRORE DURANTE LA LETTURA: {ex.Message}");
            }
        }

        private void btnModifica_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("INSERISCI IL NOME.");
                return;
            }

            if (nome.Length > DIM_NOME)
            {
                MessageBox.Show($"IL NOME PUÒ CONTENERE MASSIMO {DIM_NOME} CARATTERI.");
                return;
            }

            if (!int.TryParse(txtCodice.Text, out int codice))
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }

            try
            {
                int posizione = BinarySearch(indici, codice);

                if (posizione == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizione);

                if (!p.IsActive)
                {
                    MessageBox.Show("PRODOTTO ELIMINATO.");
                    return;
                }

                p.Nome = nome;
                p.Prezzo = nudPrezzo.Value;
                p.Quantita = (int)nudQty.Value;

                using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open, FileAccess.ReadWrite))
                {
                    ScriviProdotto(fs, p, posizione);
                }

                MessageBox.Show("PRODOTTO AGGIORNATO.");
                AggiornaDGV();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERRORE DURANTE LA MODIFICA: {ex.Message}");
            }
        }

        private void btnElimina_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtCodice.Text, out int codice))
            {
                MessageBox.Show("CODICE NON VALIDO.");
                return;
            }

            try
            {
                int posizione = BinarySearch(indici, codice);

                if (posizione == -1)
                {
                    MessageBox.Show("CODICE NON PRESENTE.");
                    return;
                }

                Prodotto p = LeggiProdotto(posizione);

                if (!p.IsActive)
                {
                    MessageBox.Show("PRODOTTO GIÀ ELIMINATO.");
                    return;
                }

                p.IsActive = false;

                using (FileStream fs = new FileStream(FILE_ARCHIVIO, FileMode.Open, FileAccess.ReadWrite))
                {
                    ScriviProdotto(fs, p, posizione);
                }

                MessageBox.Show("PRODOTTO ELIMINATO.");
                AggiornaDGV();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERRORE DURANTE L'ELIMINAZIONE: {ex.Message}");
            }
        }
    }
}
