# Checklist sécurité

## Authentification

- [ ] Le mot de passe est haché, jamais stocké en clair.
- [ ] Le message d'échec de connexion ne révèle pas si l'e-mail existe.
- [ ] Le JWT possède une expiration courte.
- [ ] La signature, l'émetteur, l'audience et l'expiration sont validés.
- [ ] HTTPS est obligatoire hors développement.

## Autorisation

- [ ] Les routes sensibles portent `[Authorize]`.
- [ ] Les actions administrateur exigent le rôle ou la politique Admin.
- [ ] Le rôle n'est jamais accepté depuis le DTO public d'inscription.
- [ ] Les contrôles d'accès sont effectués par l'API, pas seulement par le frontend.

## Secrets et déploiement

- [ ] Les secrets de développement sont dans User Secrets.
- [ ] Les secrets de production sont dans un gestionnaire de secrets ou des variables sécurisées.
- [ ] Swagger est désactivé ou protégé en production.
- [ ] La clé JWT est suffisamment longue, aléatoire et renouvelable.

## Base de données

- [ ] Le compte SQL de production possède uniquement les permissions nécessaires.
- [ ] Les migrations sont relues avant déploiement.
- [ ] Les sauvegardes sont testées.
- [ ] Les erreurs SQL détaillées ne sont pas exposées au client.
