# 🧪 Guide de tests - SalleSense

## 📋 Scripts SQL créés

Vous avez maintenant 3 scripts SQL pour configurer votre base de données de test :

### 1. ✅ Insert_3_Salles.sql
Insère 3 salles avec différentes capacités :
- **A-101** : Petite salle (25 personnes)
- **B-205** : Salle moyenne (40 personnes)
- **C-310** : Grande salle (60 personnes)

**Comment exécuter :**
```bash
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Insert_3_Salles.sql
```

---

### 2. ✅ Insert_Capteurs_Donnees.sql
Insère des capteurs et données pour chaque salle :
- **Salle A-101** : Capteur de BRUIT (niveaux sonores variés sur 24h)
- **Salle B-205** : Capteur de MOUVEMENT (détections de présence)
- **Salle C-310** : CAMÉRA (photos capturées)

**Comment exécuter :**
```bash
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Insert_Capteurs_Donnees.sql
```

---

### 3. ✅ Ajout_Role_Admin.sql
Configure le système de rôles et crée des utilisateurs de test :
- Ajoute la colonne `role` dans la table Utilisateur
- Transforme votre utilisateur existant en **Admin**
- Crée un **utilisateur test normal** pour les tests

**Identifiants créés :**
- **Admin** : tokamdaruis@gmail.com (votre mot de passe existant)
- **User** : user.test@example.com / test123

**Comment exécuter :**
```bash
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Ajout_Role_Admin.sql
```

---

## 🚀 Ordre d'exécution des scripts

Pour configurer votre environnement de test, exécutez les scripts dans cet ordre :

```bash
# 1. Créer les salles
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Insert_3_Salles.sql

# 2. Ajouter les capteurs et données
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Insert_Capteurs_Donnees.sql

# 3. Configurer les rôles Admin
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Script_bd/Ajout_Role_Admin.sql
```

**OU en une seule commande :**
```bash
cd Script_bd
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Insert_3_Salles.sql -i Insert_Capteurs_Donnees.sql -i Ajout_Role_Admin.sql
```

---

## 🧪 Scénarios de test

### Test 1 : Affichage des détails d'une salle

1. Lancez l'application avec `run-home.bat`
2. Connectez-vous avec : **tokamdaruis@gmail.com**
3. Allez dans **Salles**
4. Cliquez sur une salle (ex: A-101)
5. Vous devriez voir :
   - ✅ Informations de la salle (numéro, capacité)
   - ✅ Liste des capteurs associés
   - ✅ Données des dernières 24h (graphiques, photos, etc.)
   - ✅ Historique des activités

---

### Test 2 : Créer une réservation

1. Connectez-vous
2. Cliquez sur **Réserver** dans le menu
3. Remplissez le formulaire :
   - Sélectionnez une salle
   - Choisissez une date et heure
   - Indiquez le nombre de personnes
4. Testez les validations :
   - ❌ Nombre de personnes > capacité max → Erreur
   - ❌ Heure de fin avant heure de début → Erreur
   - ❌ Chevauchement avec une autre réservation → Erreur (trigger SQL)
5. Créez une réservation valide
6. Vérifiez qu'elle apparaît dans le dashboard

---

### Test 3 : Modifier/Supprimer une réservation

1. Allez dans **Dashboard**
2. Cliquez sur **Modifier** sur une de vos réservations
3. Modifiez les informations
4. Testez les validations (comme pour la création)
5. Sauvegardez
6. Testez aussi la suppression d'une réservation

---

### Test 4 : Gestion Admin - Blacklister un utilisateur

#### Étape 1 : Se connecter en tant qu'Admin
1. Connectez-vous avec : **tokamdaruis@gmail.com**
2. Vous devriez voir un nouveau menu : **🔐 ADMINISTRATION**
3. Cliquez sur **Gestion Utilisateurs**

#### Étape 2 : Blacklister l'utilisateur test
1. Vous voyez la liste des utilisateurs :
   - **leroi** (Vous) - Badge "Admin" 👑
   - **UserTest** - Badge "User" 👤
2. Cliquez sur **🚫 Blacklister** à côté de UserTest
3. L'utilisateur est maintenant marqué comme **Blacklisté**

#### Étape 3 : Tester que l'utilisateur blacklisté ne peut plus se connecter
1. **Déconnectez-vous**
2. Essayez de vous connecter avec : **user.test@example.com** / **test123**
3. ❌ La connexion devrait échouer avec le message : *"Votre compte a été bloqué"*

#### Étape 4 : Débloquer l'utilisateur
1. Reconnectez-vous en tant qu'Admin
2. Allez dans **Gestion Utilisateurs**
3. Cliquez sur **✓ Débloquer** à côté de UserTest
4. L'utilisateur peut maintenant se reconnecter

---

### Test 5 : Vérifier les triggers SQL

#### Test du trigger de chevauchement
1. Créez une réservation : **Salle A-101, Demain 10h-12h**
2. Essayez de créer une autre réservation : **Salle A-101, Demain 11h-13h**
3. ❌ Devrait échouer avec une erreur de chevauchement

#### Test du trigger de blacklist
1. En tant qu'Admin, blacklistez un utilisateur
2. Connectez-vous avec cet utilisateur
3. Essayez de créer une réservation
4. ❌ Devrait échouer (l'utilisateur blacklisté ne peut pas réserver)

---

## 📊 Fonctionnalités testées

### ✅ Système de réservation
- [x] Création de réservation
- [x] Modification de réservation
- [x] Suppression de réservation
- [x] Validation de capacité
- [x] Prévention des chevauchements (trigger SQL)

### ✅ Affichage des détails de salle
- [x] Informations de base (numéro, capacité)
- [x] Liste des capteurs
- [x] Données des capteurs (24h)
- [x] Graphiques/statistiques

### ✅ Système d'administration
- [x] Rôle Admin vs User
- [x] Page admin (visible seulement pour les admins)
- [x] Blacklister un utilisateur
- [x] Débloquer un utilisateur
- [x] Empêcher les blacklistés de se connecter
- [x] Protection : impossible de se blacklister soi-même
- [x] Protection : impossible de blacklister un admin

---

## 🔍 Vérifications dans la base de données

### Voir les salles créées
```sql
SELECT * FROM Salle;
```

### Voir les capteurs et leurs données
```sql
SELECT c.nom, c.type, COUNT(d.idDonnee_PK) AS NbDonnees
FROM Capteur c
LEFT JOIN Donnees d ON c.idCapteur_PK = d.idCapteur
GROUP BY c.idCapteur_PK, c.nom, c.type;
```

### Voir les utilisateurs et leurs rôles
```sql
SELECT
    u.pseudo,
    u.courriel,
    u.role,
    CASE WHEN b.idBlacklist_PK IS NOT NULL THEN 'Oui' ELSE 'Non' END AS Blackliste
FROM Utilisateur u
LEFT JOIN Blacklist b ON u.idUtilisateur_PK = b.idUtilisateur;
```

### Voir les réservations
```sql
SELECT
    r.idReservation_PK,
    s.numero AS Salle,
    u.pseudo AS ReservePar,
    r.heureDebut,
    r.heureFin,
    r.nombrePersonne
FROM Reservation r
INNER JOIN Salle s ON r.noSalle = s.idSalle_PK
INNER JOIN Utilisateur u ON r.noPersonne = u.idUtilisateur_PK
ORDER BY r.heureDebut DESC;
```

---

## 🎯 Objectifs de test atteints

1. ✅ **Tester les réservations** - Via l'interface Blazor
2. ✅ **Tester l'affichage des détails de salle** - Avec données de capteurs
3. ✅ **Implémenter un rôle admin** - Avec page de gestion
4. ✅ **Blacklister des users** - Fonctionnalité complète

---

## 💡 Prochaines étapes possibles

- [ ] Ajouter des notifications par email lors du blacklist
- [ ] Créer un système de logs des actions admin
- [ ] Ajouter des graphiques pour visualiser les données des capteurs
- [ ] Implémenter un système d'export des réservations (CSV, PDF)
- [ ] Ajouter des statistiques d'utilisation des salles
- [ ] Créer des rapports administratifs

---

## 🆘 Dépannage

### Les scripts SQL ne s'exécutent pas
```bash
# Vérifier la connexion
sqlcmd -S (localdb)\MSSQLLocalDB -Q "SELECT @@VERSION"

# Vérifier que la BD existe
sqlcmd -S (localdb)\MSSQLLocalDB -Q "SELECT name FROM sys.databases"
```

### La page Admin n'apparaît pas
1. Vérifiez que vous êtes connecté en tant qu'Admin
2. Exécutez : `SELECT * FROM Utilisateur` pour vérifier la colonne `role`
3. Si la colonne n'existe pas, exécutez `Ajout_Role_Admin.sql`

### Erreur lors du blacklist
1. Vérifiez que les foreign keys existent
2. Exécutez : `SELECT * FROM Blacklist` pour voir les entrées
3. Vérifiez les triggers avec : `SELECT * FROM sys.triggers WHERE name LIKE '%black%'`

---

Bon test! 🚀
