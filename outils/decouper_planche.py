import os
import shutil
import sys
from PIL import Image

SEUIL_SOMBRE = 100
PART_TRAIT = 0.95
TAILLE_MIN = 60
MARGE = 8
TAILLE_MAX = 1000
DOSSIER_IMAGES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ImagesGarde")
DOSSIER_PLANCHES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Planches")


def nb_sombres_ligne(noir, x0, x1, y):
    return noir.crop((x0, y, x1, y + 1)).histogram()[255]


def nb_sombres_colonne(noir, y0, y1, x):
    return noir.crop((x, y0, x + 1, y1)).histogram()[255]


def regrouper(positions):
    groupes = []
    for p in positions:
        if len(groupes) > 0 and p == groupes[-1][1] + 1:
            groupes[-1][1] = p
        else:
            groupes.append([p, p])
    return groupes


def morceaux_entre(debut, fin, traits):
    morceaux = []
    position = debut
    for t in traits:
        if t[0] - position >= TAILLE_MIN:
            morceaux.append((position, t[0]))
        position = t[1] + 1
    if fin - position >= TAILLE_MIN:
        morceaux.append((position, fin))
    return morceaux


def chercher_traits_horizontaux(noir, x0, y0, x1, y1):
    largeur = x1 - x0
    lignes = []
    for y in range(y0, y1):
        if nb_sombres_ligne(noir, x0, x1, y) >= largeur * PART_TRAIT:
            lignes.append(y)
    return regrouper(lignes)


def chercher_traits_verticaux(noir, x0, y0, x1, y1):
    hauteur = y1 - y0
    colonnes = []
    for x in range(x0, x1):
        if nb_sombres_colonne(noir, y0, y1, x) >= hauteur * PART_TRAIT:
            colonnes.append(x)
    return regrouper(colonnes)


def decouper_zone(noir, x0, y0, x1, y1, cases):
    traits = chercher_traits_horizontaux(noir, x0, y0, x1, y1)
    morceaux = morceaux_entre(y0, y1, traits)
    if len(traits) > 0 and (len(morceaux) > 1 or morceaux_reduits(morceaux, y0, y1)):
        for m in morceaux:
            decouper_zone(noir, x0, m[0], x1, m[1], cases)
        return

    traits = chercher_traits_verticaux(noir, x0, y0, x1, y1)
    morceaux = morceaux_entre(x0, x1, traits)
    if len(traits) > 0 and (len(morceaux) > 1 or morceaux_reduits(morceaux, x0, x1)):
        for m in morceaux:
            decouper_zone(noir, m[0], y0, m[1], y1, cases)
        return

    cases.append((x0, y0, x1, y1))


def morceaux_reduits(morceaux, debut, fin):
    if len(morceaux) != 1:
        return False
    return morceaux[0][0] != debut or morceaux[0][1] != fin


def decouper_grille(largeur, hauteur, colonnes, lignes):
    cases = []
    for l in range(lignes):
        for c in range(colonnes):
            x0 = largeur * c // colonnes
            x1 = largeur * (c + 1) // colonnes
            y0 = hauteur * l // lignes
            y1 = hauteur * (l + 1) // lignes
            cases.append((x0, y0, x1, y1))
    return cases


def recadrer(image, noir, case):
    x0, y0, x1, y1 = case
    bord = 10
    zone = noir.crop((x0 + bord, y0 + bord, x1 - bord, y1 - bord))
    contenu = zone.getbbox()
    if contenu is None:
        return None
    cx0 = max(x0 + bord, x0 + bord + contenu[0] - MARGE)
    cy0 = max(y0 + bord, y0 + bord + contenu[1] - MARGE)
    cx1 = min(x1 - bord, x0 + bord + contenu[2] + MARGE)
    cy1 = min(y1 - bord, y0 + bord + contenu[3] + MARGE)
    return image.crop((cx0, cy0, cx1, cy1))


def fond_transparent(image):
    image = image.convert("RGBA")
    transparence = image.convert("L").point(lambda v: 0 if v > 235 else 255)
    image.putalpha(transparence)
    return image


def prochain_numero(dossier, categorie):
    numero = 1
    while os.path.exists(os.path.join(dossier, categorie + " " + str(numero) + ".png")):
        numero = numero + 1
    return numero


def main():
    if len(sys.argv) < 3:
        print('Utilisation : python decouper_planche.py "planche.jpg" "Categorie" [--grille 3x2]')
        return

    chemin = sys.argv[1]
    categorie = sys.argv[2]
    grille = None
    if len(sys.argv) >= 5 and sys.argv[3] == "--grille":
        morceaux = sys.argv[4].lower().split("x")
        grille = (int(morceaux[0]), int(morceaux[1]))

    image = Image.open(chemin).convert("RGB")
    gris = image.convert("L")
    noir = gris.point(lambda v: 255 if v < SEUIL_SOMBRE else 0)

    if grille is None:
        cases = []
        decouper_zone(noir, 0, 0, image.width, image.height, cases)
    else:
        cases = decouper_grille(image.width, image.height, grille[0], grille[1])

    dossier = os.path.join(DOSSIER_IMAGES, categorie)
    os.makedirs(dossier, exist_ok=True)

    numero = prochain_numero(dossier, categorie)
    enregistrees = 0
    for case in cases:
        morceau = recadrer(image, noir, case)
        if morceau is None:
            continue
        morceau.thumbnail((TAILLE_MAX, TAILLE_MAX))
        morceau = fond_transparent(morceau)
        nom = categorie + " " + str(numero) + ".png"
        morceau.save(os.path.join(dossier, nom))
        print("  " + nom + "  (" + str(morceau.width) + "x" + str(morceau.height) + ")")
        numero = numero + 1
        enregistrees = enregistrees + 1

    dossier_planche = os.path.join(DOSSIER_PLANCHES, categorie)
    os.makedirs(dossier_planche, exist_ok=True)
    shutil.copyfile(chemin, os.path.join(dossier_planche, os.path.basename(chemin).strip()))

    print(str(len(cases)) + " cases trouvées, " + str(enregistrees) + " images enregistrées dans " + dossier)
    print("Planche d'origine copiée dans " + dossier_planche)


main()
