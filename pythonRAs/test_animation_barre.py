"""
Script de test pour visualiser l'animation de la barre de son
Fonctionne sans base de données - génère des valeurs aléatoires
"""

import tkinter as tk
import random
import time


class TestAnimationBarre:
    """Test de l'animation de la barre de son"""

    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Test Animation Barre de Son - SalleSense")
        self.root.geometry("800x400")

        # Couleurs
        self.colors = {
            'primary': '#2563eb',
            'secondary': '#8b5cf6',
            'success': '#10b981',
            'danger': '#ef4444',
            'warning': '#f59e0b',
            'dark': '#1e293b',
            'light': '#f8fafc',
            'gray': '#64748b',
            'bg': '#f1f5f9',
            'card': '#ffffff',
            'border': '#e2e8f0'
        }

        self.root.configure(bg=self.colors['bg'])

        # Variables
        self.en_cours = True
        self.niveau_son_actuel = 0
        self.niveau_son_cible = 0

        # Interface
        self.creer_interface()

        # Lancer l'animation
        self.animer_barre_son()
        self.generer_valeurs_aleatoires()

        self.root.protocol("WM_DELETE_WINDOW", self.fermer)

    def creer_interface(self):
        """Crée l'interface de test"""
        # Header
        header = tk.Frame(self.root, bg=self.colors['primary'], height=60)
        header.pack(fill=tk.X)
        header.pack_propagate(False)

        tk.Label(header, text='🎤 Test Animation Barre de Son - SalleSense',
                font=('Arial', 16, 'bold'),
                fg='white', bg=self.colors['primary']).pack(pady=15)

        # Container principal
        main_frame = tk.Frame(self.root, bg=self.colors['bg'], padx=50, pady=30)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Carte
        card = tk.Frame(main_frame, bg=self.colors['card'], relief=tk.RAISED, borderwidth=2)
        card.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        content = tk.Frame(card, bg=self.colors['card'], padx=30, pady=20)
        content.pack(fill=tk.BOTH, expand=True)

        # Titre
        tk.Label(content, text="🎤 Niveau Sonore",
                font=('Arial', 14, 'bold'),
                fg=self.colors['dark'], bg=self.colors['card']).pack(pady=(0, 10))

        # Valeur numérique
        self.son_value_label = tk.Label(content, text="0.0 dB",
                                        font=('Arial', 40, 'bold'),
                                        fg=self.colors['primary'],
                                        bg=self.colors['card'])
        self.son_value_label.pack(pady=10)

        # Canvas pour barre animée
        self.son_canvas = tk.Canvas(content, bg=self.colors['border'],
                                   height=40, highlightthickness=0)
        self.son_canvas.pack(fill=tk.X, pady=10)

        # Seuils
        seuils_frame = tk.Frame(content, bg=self.colors['card'])
        seuils_frame.pack(fill=tk.X, pady=5)

        tk.Label(seuils_frame, text="0 dB", font=('Arial', 8),
                fg=self.colors['gray'], bg=self.colors['card']).pack(side=tk.LEFT)
        tk.Label(seuils_frame, text="50 dB (Calme)", font=('Arial', 8),
                fg=self.colors['success'], bg=self.colors['card']).pack(side=tk.LEFT, padx=80)
        tk.Label(seuils_frame, text="70 dB (Bruyant)", font=('Arial', 8),
                fg=self.colors['warning'], bg=self.colors['card']).pack(side=tk.LEFT, padx=80)
        tk.Label(seuils_frame, text="100 dB", font=('Arial', 8),
                fg=self.colors['danger'], bg=self.colors['card']).pack(side=tk.RIGHT)

        # Info
        tk.Label(content, text="Les valeurs sont générées aléatoirement pour la démonstration",
                font=('Arial', 10, 'italic'),
                fg=self.colors['gray'],
                bg=self.colors['card']).pack(pady=(20, 0))

    def animer_barre_son(self):
        """Anime la barre de son avec transition fluide"""
        if not self.en_cours:
            return

        try:
            # Interpolation fluide vers la valeur cible
            diff = self.niveau_son_cible - self.niveau_son_actuel
            if abs(diff) > 0.5:
                # Animation progressive
                self.niveau_son_actuel += diff * 0.3
            else:
                self.niveau_son_actuel = self.niveau_son_cible

            # Mettre à jour le label
            self.son_value_label.config(text=f"{self.niveau_son_actuel:.1f} dB")

            # Couleur selon le niveau
            if self.niveau_son_actuel > 70:
                self.son_value_label.config(fg=self.colors['danger'])
            elif self.niveau_son_actuel > 50:
                self.son_value_label.config(fg=self.colors['warning'])
            else:
                self.son_value_label.config(fg=self.colors['success'])

            # Récupérer la largeur du canvas
            canvas_width = self.son_canvas.winfo_width()
            if canvas_width <= 1:
                canvas_width = 600

            canvas_height = 40

            # Effacer le canvas
            self.son_canvas.delete("all")

            # Dessiner les zones de fond (seuils)
            width_50 = int(canvas_width * 0.5)
            width_70 = int(canvas_width * 0.7)

            # Zone verte (0-50 dB)
            self.son_canvas.create_rectangle(0, 0, width_50, canvas_height,
                                            fill='#d1fae5', outline='')
            # Zone orange (50-70 dB)
            self.son_canvas.create_rectangle(width_50, 0, width_70, canvas_height,
                                            fill='#fed7aa', outline='')
            # Zone rouge (70-100 dB)
            self.son_canvas.create_rectangle(width_70, 0, canvas_width, canvas_height,
                                            fill='#fecaca', outline='')

            # Calculer la largeur de la barre actuelle
            bar_width = int((min(100, self.niveau_son_actuel) / 100) * canvas_width)

            # Choisir la couleur selon le niveau
            if self.niveau_son_actuel > 70:
                bar_color = self.colors['danger']
            elif self.niveau_son_actuel > 50:
                bar_color = self.colors['warning']
            else:
                bar_color = self.colors['success']

            # Dessiner la barre principale
            if bar_width > 0:
                # Barre principale
                self.son_canvas.create_rectangle(0, 0, bar_width, canvas_height,
                                                fill=bar_color, outline='')

                # Effet de brillance (gradient supérieur)
                gradient_height = int(canvas_height * 0.4)
                self.son_canvas.create_rectangle(0, 0, bar_width, gradient_height,
                                                fill='white', outline='', stipple='gray50')

                # Bordure de la barre
                self.son_canvas.create_rectangle(0, 0, bar_width, canvas_height,
                                                outline=bar_color, width=2)

            # Marqueurs de seuils
            line_50 = int(canvas_width * 0.5)
            self.son_canvas.create_line(line_50, 0, line_50, canvas_height,
                                       fill=self.colors['success'], width=2, dash=(5, 5))

            line_70 = int(canvas_width * 0.7)
            self.son_canvas.create_line(line_70, 0, line_70, canvas_height,
                                       fill=self.colors['danger'], width=2, dash=(5, 5))

            # Pics d'amplitude si son fort
            if self.niveau_son_actuel > 60 and bar_width > 10:
                for _ in range(3):
                    x = random.randint(5, max(6, bar_width - 10))
                    self.son_canvas.create_oval(x, 5, x+10, 15,
                                               fill='white', outline='', stipple='gray25')

        except Exception as e:
            print(f"Erreur animation: {e}")

        # Répéter l'animation (60 FPS)
        if self.en_cours:
            self.root.after(16, self.animer_barre_son)

    def generer_valeurs_aleatoires(self):
        """Génère des valeurs aléatoires pour simuler des mesures"""
        if not self.en_cours:
            return

        # Générer une nouvelle valeur cible
        # Parfois calme (0-50), parfois moyen (50-70), parfois fort (70-100)
        rand = random.random()
        if rand < 0.5:
            # Calme
            self.niveau_son_cible = random.uniform(10, 50)
        elif rand < 0.8:
            # Moyen
            self.niveau_son_cible = random.uniform(50, 70)
        else:
            # Fort
            self.niveau_son_cible = random.uniform(70, 95)

        # Générer une nouvelle valeur toutes les 500ms
        self.root.after(500, self.generer_valeurs_aleatoires)

    def fermer(self):
        """Ferme l'application"""
        self.en_cours = False
        self.root.destroy()

    def run(self):
        """Lance l'application"""
        self.root.mainloop()


if __name__ == "__main__":
    print("\n" + "="*60)
    print("  Test Animation Barre de Son - SalleSense")
    print("="*60 + "\n")
    print("Animation de la barre de son en temps réel")
    print("Les valeurs sont générées aléatoirement\n")
    print("Fermez la fenêtre pour arrêter...\n")

    app = TestAnimationBarre()
    app.run()
