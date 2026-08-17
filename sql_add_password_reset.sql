-- ============================================================
-- Ajoute les colonnes reset_token / reset_token_expires_at a la
-- table utilisateur pour la procedure "mot de passe oublie"
-- (POST /api/auth/forgot-password puis POST /api/auth/reset-password).
--
-- IMPORTANT: A EXECUTER SUR LA BD PROD AVANT DE DEPLOYER LE BACKEND.
--   Sans ces colonnes, /api/auth/login lui-meme echouerait : EF Core
--   selectionne toutes les colonnes mappees de l'entite Utilisateur.
--
-- Script idempotent : peut etre relance sans risque.
-- ============================================================

-- Jeton a usage unique envoye par mail (64 caracteres hexadecimaux).
ALTER TABLE public.utilisateur
    ADD COLUMN IF NOT EXISTS reset_token character varying(100) NULL;

-- Expiration du jeton : 1 heure apres la demande (UTC, sans fuseau horaire).
ALTER TABLE public.utilisateur
    ADD COLUMN IF NOT EXISTS reset_token_expires_at timestamp without time zone NULL;

-- Index sur le jeton : la validation et la reinitialisation cherchent
-- l'utilisateur par reset_token. Index partiel (les comptes sans demande
-- en cours ont reset_token a NULL et n'ont pas a etre indexes).
CREATE INDEX IF NOT EXISTS idx_utilisateur_reset_token
    ON public.utilisateur(reset_token)
    WHERE reset_token IS NOT NULL;
