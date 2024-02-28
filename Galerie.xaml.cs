using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.Threading;

namespace BodyMove
{

	/// <summary>
	/// Logique d'interaction pour Galerie.xaml
	/// </summary>
	public partial class Galerie : Window
	{
		KinectSensor MaKinect;

		/// <summary>
		/// Taille d'un pixel en RGB en octets
		/// </summary>
		private int octetsParPixel = (PixelFormats.Bgr32.BitsPerPixel) / 8;

		/// <summary>
		/// Variable pour contenir un frame d'image
		/// </summary>
		private ColorFrameReader lecteurCouleur = null;

		/// <summary>
		/// Un tableau de pixels
		/// </summary>
		private byte[] pixels = null;

		/// <summary>
		/// Une image couleur qui sera liée à l'interface
		/// </summary>
		private WriteableBitmap BitmapCouleur = null;

		
		private Body[] bodies = null;

		/// <summary>
		/// FrameReader pour les joueurs
		/// </summary>
		private BodyFrameReader lecteurBody = null;

		/// <summary>
		/// Timer indiquant le temps qu'il reste pour faire la pose
		/// </summary>
		private DispatcherTimer timer = new DispatcherTimer();

		/// <summary>
		/// Sert à la reconnaissance vocale
		/// </summary>
		private SpeechRecognitionEngine engine = new SpeechRecognitionEngine();

		/// <summary>
		/// Temps de pause entre 2 images
		/// </summary>
		private int temps = 2;

		/// <summary>
		/// Index de la photo a afficher
		/// </summary>
		private int indexPhoto = 0;

		/// <summary>
		/// Indique si le temps entre 2 photos est écoulé et donc si on peut changer de photo
		/// </summary>
		private bool tempsFini = false;

		/// <summary>
		/// Sert à avoir toutes les informations d'un dossier
		/// </summary>
		private DirectoryInfo dossierPhotos;

		/// <summary>
		/// Sert à avoir les informations des fichiers photos;
		/// </summary>
		private FileInfo[] fichiersPhotos;

		/// <summary>
		/// Pour qu'il n'y ai pas plusieurs fenêtre qui s'ouvre
		/// On a remarqué que quand on fait this.Close() le code continue a s'executer
		/// Cette variable permet donc de controler l'execution.
		/// </summary>
		private bool fenetreOuverte = false;

		public Galerie()
		{
			InitializeComponent();
			DemarrerKinect();

			/* Gestion du timer */
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += timer_Tick;
			timer.Start();

			/* Affichage de la première photo */
			dossierPhotos = new DirectoryInfo("Images");
			fichiersPhotos = dossierPhotos.GetFiles();
			indexPhoto = fichiersPhotos.Length - 1;
			imgPhoto.Source = new BitmapImage(new Uri(fichiersPhotos[indexPhoto].FullName));
			
			/* Gestion de la reconnaissance vocale */

			//On entre une liste de mots a reconnaitre (ici nous avons rajouter un mot pour que la reconnaissance soit un peu plus précise, plus il y a de mots plus elle est précise
			Choices choixDeMots = new Choices(new string[] { "licorne", "menu" });
			Grammar grammaire = new Grammar(new GrammarBuilder(choixDeMots));
			try
			{
				//On met à jour la reconnaissance
				engine.RequestRecognizerUpdate();
				//On lui attribue les mots a reconnaitre
				engine.LoadGrammar(grammaire);
				//On crée un événement pour quand un des mots de la liste est reconnu
				engine.SpeechRecognized += engine_SpeechRecognized;
				//On définit l'entrée audio par défaut
				engine.SetInputToDefaultAudioDevice();
				//On lance la reconnaissance en asynchrone 
				engine.RecognizeAsync(RecognizeMode.Multiple);
				engine.Recognize();
			}
			catch
			{
				return;
			}
		}

		/// <summary>
		/// Reconnaissance vocale
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			try
			{
				//On récupère le mot qui à été détecté
				string mot = e.Result.Text;

				//Si le mot correspond au menu et que le temps d'attente est fini alors on ouvre la form de menu
				if(mot == "menu" && !fenetreOuverte) { 
					MenuDebut menu = new MenuDebut();
					this.Close();
					menu.ShowDialog();
					tempsFini = false;
					fenetreOuverte = true;
				}
			}
			catch (Exception)
			{
				return;
			}
		}

		/// <summary>
		/// Evenement du timer, le timer évite que lorsqu'on ferme la main les photos défiles trop vite
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void timer_Tick(object sender, EventArgs e)
		{
			if (!fenetreOuverte) { 
				//On décrémente le temps jusqu'a 0
				temps--;
				if (temps == 0)
				{
					tempsFini = true;
				}
			}
		}

		/// <summary>
		/// Affichage des photos
		/// </summary>
		private void afficherPhoto()
		{		
			//Vérification qu'il y a bien une photo à afficher et que le timer est fini
			if (fichiersPhotos.Length > indexPhoto && indexPhoto >= 0 && tempsFini)
			{
				/* Animation pour faire un effet de fondue des photos, ici on fait disparaitre la photo a l'écran */
				DoubleAnimation animOp = new DoubleAnimation();
				animOp.From = 1;
				animOp.To = 0;
				animOp.Duration = TimeSpan.FromSeconds(1);
				animOp.Completed += AnimOp_Completed;
				imgPhoto.BeginAnimation(OpacityProperty, animOp);

				tempsFini = false;
			}
		}

		/// <summary>
		/// Evenement lorsque l'animation est terminée
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AnimOp_Completed(object sender, EventArgs e)
		{
			//On change de source afin de changer de photo
			imgPhoto.Source = new BitmapImage(new Uri(fichiersPhotos[indexPhoto].FullName));

			//On met une animation pour que la nouvelle photo apparaisse en fondue
			DoubleAnimation animOp = new DoubleAnimation();
			animOp.From = 0;
			animOp.To = 1;
			animOp.Duration = TimeSpan.FromSeconds(1);
			imgPhoto.BeginAnimation(OpacityProperty, animOp);
		}

		
		void DemarrerKinect()
		{
			// Vérification capteurs kinect
			MaKinect = KinectSensor.GetDefault();
			MaKinect.Open();
			if (MaKinect != null)
			{
				initialiserBody();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (MaKinect != null)
			{
				MaKinect.Close();
			}
		}

		void initialiserBody()
		{
			// Allouer la liste
			bodies = new Body[MaKinect.BodyFrameSource.BodyCount];

			// Ouvrir le lecteur
			lecteurBody = MaKinect.BodyFrameSource.OpenReader();

			// Associer l'événement
			lecteurBody.FrameArrived += lecteurBody_FrameArrived;
		}

		void lecteurBody_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			BodyFrameReference refer = e.FrameReference;

			if (refer == null) return;

			// Obtenir body frame
			BodyFrame frame = refer.AcquireFrame();

			if (frame == null) return;

			using (frame)
			{
				// Obtenir les données des joueurs
				frame.GetAndRefreshBodyData(bodies);

				// Clear Skeleton Canvas
				CanvasCameraPrincipale.Children.Clear();

				// Loop all bodies
				foreach (Body body in bodies)
				{
					// Only process tracked bodies and if the timer is over
					if (body.IsTracked && tempsFini)
					{
						if(body.HandLeftState == HandState.Closed) //Si la main gauche est fermée on met l'image d'avant
						{
							indexPhoto--;
							afficherPhoto();
							//Une fois à 0 on le remet à 3
							temps = 2;
							tempsFini = false;
						}
						else if(body.HandRightState == HandState.Closed) //Si la main droite est fermée on met l'image d'après
						{
							indexPhoto++;
							afficherPhoto();
							//Une fois à 0 on le remet à 3
							temps = 2;
							tempsFini = false;
						}
					}
				}
			}
		}
	}
}
