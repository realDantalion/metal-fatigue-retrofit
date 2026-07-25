// Metal Fatigue Retrofit
// Copyright (C) 2026 Dantalion (github.com/realDantalion)
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Globalization;

namespace MetalFatiguePatcher
{
    /// <summary>
    /// Minimal built-in localization. String tables live in code (not .resx satellite
    /// assemblies) so the patcher stays a single self-contained .exe.
    /// Language is auto-detected from the Windows UI culture; English is the fallback.
    /// Adding a language = add one column to every entry + extend Names/Codes + a flag PNG.
    /// Column order: EN, DE, ES, PT, IT, FR, JA, KO, ZH, RU  (10 = a 5x2 flag grid)
    /// </summary>
    public static class Lang
    {
        public enum L
        {
            EN = 0, DE = 1, ES = 2, PT = 3, IT = 4,
            FR = 5, JA = 6, KO = 7, ZH = 8, RU = 9
        }

        public static readonly string[] Names =
            { "English", "Deutsch", "Español", "Português", "Italiano",
              "Français", "日本語", "한국어", "简体中文", "Русский" };
        public static readonly string[] Codes =
            { "en", "de", "es", "pt", "it", "fr", "ja", "ko", "zh", "ru" };

        public static L Current = Detect();

        static L Detect()
        {
            var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            for (int i = 0; i < Codes.Length; i++)
                if (Codes[i] == two) return (L)i;
            return L.EN;
        }

        public static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            var i = (int)Current;
            return i < v.Length && !string.IsNullOrEmpty(v[i]) ? v[i] : v[0];
        }

        public static string ProfileTitle(string key) => T("prof." + key + ".title");
        public static string ProfileDesc(string key)  => T("prof." + key + ".desc");

        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            { "window.title", new[]{
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit" } },

            { "banner.title", new[]{
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit",
                "Metal Fatigue Retrofit" } },

            { "banner.sub", new[]{
                "Fixes the unit limit and the crew-name limit",
                "Behebt das Einheiten-Limit und das Crew-Namen-Limit",
                "Corrige el límite de unidades y el de nombres de tripulación",
                "Corrige o limite de unidades e o de nomes de tripulação",
                "Corregge il limite di unità e quello dei nomi dell'equipaggio",
                "Corrige la limite d'unités et celle des noms d'équipage",
                "ユニット上限とクルー名の上限を修正します",
                "유닛 제한과 크루 이름 제한을 수정합니다",
                "修复单位上限与机组名称上限",
                "Исправляет лимит юнитов и лимит имён экипажей" } },

            { "banner.cheatTitle", new[]{
                "⚡ CHEAT MODE",
                "⚡ CHEAT-MODUS",
                "⚡ MODO TRUCOS",
                "⚡ MODO TRAPAÇA",
                "⚡ MODALITÀ TRUCCHI",
                "⚡ MODE TRICHE",
                "⚡ チートモード",
                "⚡ 치트 모드",
                "⚡ 作弊模式",
                "⚡ РЕЖИМ ЧИТОВ" } },

            { "banner.cheatSub", new[]{
                "Free building · turbo build · no fog of war",
                "Gratis bauen · Turbo-Bau · kein Nebel des Krieges",
                "Construcción gratis · construcción turbo · sin niebla de guerra",
                "Construção grátis · construção turbo · sem névoa de guerra",
                "Costruzione gratuita · costruzione turbo · niente nebbia",
                "Construction gratuite · construction turbo · sans brouillard de guerre",
                "無料建設・高速建設・戦場の霧なし",
                "무료 건설 · 초고속 건설 · 전쟁의 안개 없음",
                "免费建造 · 极速建造 · 无战争迷雾",
                "Бесплатная постройка · турбо-строительство · без тумана войны" } },

            { "banner.expSub", new[]{
                "Experimental features — may break things",
                "Experimentelle Features — kann Dinge kaputt machen",
                "Funciones experimentales — pueden romper cosas",
                "Recursos experimentais — podem quebrar coisas",
                "Funzioni sperimentali — possono rompere qualcosa",
                "Fonctions expérimentales — peuvent tout casser",
                "実験的機能 — 不具合が出ることがあります",
                "실험적 기능 — 문제가 생길 수 있습니다",
                "实验性功能 — 可能出问题",
                "Экспериментальные функции — могут что-то сломать" } },

            { "grp.source", new[]{
                "1. Game source",
                "1. Spielquelle",
                "1. Origen del juego",
                "1. Origem do jogo",
                "1. Origine del gioco",
                "1. Source du jeu",
                "1. ゲームの入手元",
                "1. 게임 출처",
                "1. 游戏来源",
                "1. Источник игры" } },

            { "src.auto", new[]{
                "Auto-detect",
                "Automatisch erkennen",
                "Detección automática",
                "Detecção automática",
                "Rilevamento automatico",
                "Détection automatique",
                "自動検出",
                "자동 감지",
                "自动检测",
                "Автоопределение" } },

            { "lbl.exe", new[]{
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:",
                "MFatigue.exe:" } },

            { "btn.browse", new[]{
                "Browse…",
                "Durchsuchen…",
                "Examinar…",
                "Procurar…",
                "Sfoglia…",
                "Parcourir…",
                "参照…",
                "찾아보기…",
                "浏览…",
                "Обзор…" } },

            { "sv.label", new[]{
                "Share vision with allied players",
                "Gemeinsame Sicht mit Verbündeten",
                "Compartir visión con aliados",
                "Compartilhar visão com aliados",
                "Condividi la visuale con gli alleati",
                "Partager la vision avec les alliés",
                "同盟プレイヤーと視界を共有",
                "동맹 플레이어와 시야 공유",
                "与盟友共享视野",
                "Общий обзор с союзниками" } },

            { "sv.hint", new[]{
                "You also see what your allies see. Purely local rendering — but every player must run the same build.",
                "Du siehst auch, was deine Verbündeten sehen. Rein lokale Darstellung — aber alle Spieler müssen denselben Build nutzen.",
                "También ves lo que ven tus aliados. Solo afecta a tu vista, pero todos deben usar la misma versión.",
                "Você também vê o que seus aliados veem. Apenas visual local, mas todos devem usar a mesma versão.",
                "Vedi anche ciò che vedono i tuoi alleati. Effetto puramente locale — ma tutti i giocatori devono usare la stessa build.",
                "Vous voyez aussi ce que voient vos alliés. Rendu purement local, mais tous doivent utiliser la même version.",
                "同盟軍が見ているものも見えます。表示のみのローカル処理ですが、全員が同じビルドを使う必要があります。",
                "동맹이 보는 것도 함께 보입니다. 화면 표시만 바뀌는 로컬 처리이지만, 모든 플레이어가 같은 빌드를 실행해야 합니다.",
                "你也能看到盟友所见。仅影响本地显示，但所有玩家须使用同一版本。",
                "Вы также видите то, что видят союзники. Только локальное отображение, но у всех должна быть одна сборка." } },

            { "sv.on", new[]{
                "● currently active",
                "● derzeit aktiv",
                "● actualmente activo",
                "● atualmente ativo",
                "● attualmente attivo",
                "● actuellement actif",
                "● 現在オン",
                "● 현재 활성화됨",
                "● 当前已启用",
                "● сейчас включено" } },

            { "sv.off", new[]{
                "○ currently inactive",
                "○ derzeit inaktiv",
                "○ actualmente inactivo",
                "○ atualmente inativo",
                "○ attualmente inattivo",
                "○ actuellement inactif",
                "○ 現在オフ",
                "○ 현재 비활성화됨",
                "○ 当前未启用",
                "○ сейчас выключено" } },

            { "grp.version", new[]{
                "2. Version",
                "2. Version",
                "2. Versión",
                "2. Versão",
                "2. Versione",
                "2. Version",
                "2. バージョン",
                "2. 버전",
                "2. 版本",
                "2. Версия" } },

            { "grp.sharedvision", new[]{
                "3. Shared vision",
                "3. Gemeinsame Sicht",
                "3. Visión compartida",
                "3. Visão compartilhada",
                "3. Visione condivisa",
                "3. Vision partagée",
                "3. 視界の共有",
                "3. 시야 공유",
                "3. 共享视野",
                "3. Общий обзор" } },

            { "btn.patch", new[]{
                "Patch",
                "Patchen",
                "Aplicar parche",
                "Aplicar patch",
                "Applica patch",
                "Appliquer",
                "パッチ適用",
                "패치",
                "应用补丁",
                "Патчить" } },

            { "btn.restore", new[]{
                "Restore original",
                "Original wiederherstellen",
                "Restaurar original",
                "Restaurar original",
                "Ripristina originale",
                "Restaurer l'original",
                "オリジナルに戻す",
                "원본 복원",
                "还原原版",
                "Восстановить оригинал" } },

            { "btn.exit", new[]{
                "Exit",
                "Beenden",
                "Salir",
                "Sair",
                "Esci",
                "Quitter",
                "終了",
                "종료",
                "退出",
                "Выход" } },

            { "compat.exact", new[]{
                "✔ Supported game version detected — ready to patch.",
                "✔ Unterstützte Spielversion erkannt — bereit zum Patchen.",
                "✔ Versión del juego compatible detectada: lista para parchear.",
                "✔ Versão compatível detectada — pronto para aplicar o patch.",
                "✔ Rilevata una versione del gioco supportata — pronto per la patch.",
                "✔ Version du jeu prise en charge détectée — prêt à patcher.",
                "✔ 対応バージョンを検出しました — パッチ適用できます。",
                "✔ 지원되는 게임 버전을 감지했습니다 — 패치할 수 있습니다.",
                "✔ 已检测到受支持的游戏版本 — 可以打补丁。",
                "✔ Обнаружена поддерживаемая версия игры — можно патчить." } },

            { "compat.ok", new[]{
                "✔ Compatible game version — ready to patch.",
                "✔ Kompatible Spielversion — bereit zum Patchen.",
                "✔ Versión del juego compatible: lista para parchear.",
                "✔ Versão compatível — pronto para aplicar o patch.",
                "✔ Versione del gioco compatibile — pronto per la patch.",
                "✔ Version du jeu compatible — prêt à patcher.",
                "✔ 互換性のあるバージョンです — パッチ適用できます。",
                "✔ 호환되는 게임 버전입니다 — 패치할 수 있습니다.",
                "✔ 兼容的游戏版本 — 可以打补丁。",
                "✔ Совместимая версия игры — можно патчить." } },

            { "compat.patched", new[]{
                "● Already patched — you can switch version or restore the original.",
                "● Bereits gepatcht — Version wechselbar, Original wiederherstellbar.",
                "● Ya parcheado: puedes cambiar de versión o restaurar el original.",
                "● Já aplicado — pode trocar de versão ou restaurar o original.",
                "● Patch già applicata — puoi cambiare versione o ripristinare l'originale.",
                "● Déjà patché — version modifiable, original restaurable.",
                "● パッチ適用済み — バージョン変更・復元が可能です。",
                "● 패치 적용됨 — 버전을 변경하거나 원본으로 복원할 수 있습니다.",
                "● 已打补丁 — 可切换版本或还原原版。",
                "● Уже пропатчено — можно сменить версию или восстановить оригинал." } },

            { "compat.patchedUnknown", new[]{
                "● Already patched — you can switch version or restore the original.",
                "● Bereits gepatcht — Version wechselbar, Original wiederherstellbar.",
                "● Ya parcheado: puedes cambiar de versión o restaurar el original.",
                "● Já aplicado — pode trocar de versão ou restaurar o original.",
                "● Patch già applicata — puoi cambiare versione o ripristinare l'originale.",
                "● Déjà patché — version modifiable, original restaurable.",
                "● パッチ適用済み — バージョン変更・復元が可能です。",
                "● 패치 적용됨 — 버전을 변경하거나 원본으로 복원할 수 있습니다.",
                "● 已打补丁 — 可切换版本或还原原版。",
                "● Уже пропатчено — можно сменить версию или восстановить оригинал." } },

            { "compat.unsupported", new[]{
                "⚠ Not compatible with this patch (unknown version).",
                "⚠ Nicht kompatibel mit diesem Patch (unbekannte Version).",
                "⚠ No compatible con este parche (versión desconocida).",
                "⚠ Não compatível com este patch (versão desconhecida).",
                "⚠ Non compatibile con questa patch (versione sconosciuta).",
                "⚠ Non compatible avec ce patch (version inconnue).",
                "⚠ このパッチに対応していません（不明なバージョン）。",
                "⚠ 이 패치와는 호환되지 않습니다(알 수 없는 버전).",
                "⚠ 与此补丁不兼容（未知版本）。",
                "⚠ Несовместимо с этим патчем (неизвестная версия)." } },

            { "compat.patchedNoBackup", new[]{
                "⚠ Patched, but the backup is missing — verify or reinstall the game to get a clean copy.",
                "⚠ Gepatcht, aber das Backup fehlt — Spiel verifizieren/neu installieren für eine saubere Kopie.",
                "⚠ Parcheado, pero falta la copia de seguridad: verifica o reinstala el juego.",
                "⚠ Já aplicado, mas o backup sumiu — verifique ou reinstale o jogo.",
                "⚠ Patch applicata, ma manca il backup — verifica o reinstalla il gioco per avere una copia pulita.",
                "⚠ Patché, mais la sauvegarde manque — vérifiez ou réinstallez le jeu.",
                "⚠ パッチ済みですがバックアップがありません — ゲームを検証／再インストールしてください。",
                "⚠ 패치되었지만 백업이 없습니다 — 게임을 검증하거나 재설치해 깨끗한 사본을 확보해 주세요.",
                "⚠ 已打补丁，但备份丢失 — 请验证或重装游戏以获得干净副本。",
                "⚠ Пропатчено, но резервная копия отсутствует — проверьте или переустановите игру." } },

            { "info.legacyHint", new[]{
                "Note: patched by Retrofit {0}; re-patching is recommended — it rebuilds from your backup.",
                "Hinweis: mit Retrofit {0} gepatcht; neu patchen empfohlen — baut automatisch aus dem Backup neu.",
                "Nota: parcheado con Retrofit {0}; se recomienda volver a parchear: se reconstruye desde tu copia.",
                "Nota: aplicado com o Retrofit {0}; recomenda-se aplicar de novo — reconstrói a partir do backup.",
                "Nota: patch applicata con Retrofit {0}; conviene riapplicarla — ricostruisce dal backup.",
                "Note : patché avec Retrofit {0} ; il est conseillé de repatcher — reconstruit depuis la sauvegarde.",
                "注意: Retrofit {0} でパッチ済みです。バックアップから再構築されるため、再パッチを推奨します。",
                "참고: Retrofit {0}(으)로 패치되었습니다. 백업에서 다시 만들므로 재패치를 권장합니다.",
                "提示：由 Retrofit {0} 打过补丁；建议重新打补丁 — 会自动从备份重建。",
                "Примечание: пропатчено Retrofit {0}; рекомендуется пропатчить заново — сборка идёт из резервной копии." } },

            { "compat.legacyLayout", new[]{
                "⚠ Patched by Retrofit {0}, whose layout is no longer supported, and the backup is missing — restore the original (verify/reinstall the game), then patch again.",
                "⚠ Mit Retrofit {0} gepatcht, dessen Layout nicht mehr unterstützt wird, und das Backup fehlt — bitte das Original wiederherstellen (Spiel verifizieren/neu installieren) und erneut patchen.",
                "⚠ Parcheado con Retrofit {0}, cuyo diseño ya no es compatible, y falta la copia de seguridad: restaura el original (verifica o reinstala el juego) y vuelve a parchear.",
                "⚠ Aplicado com o Retrofit {0}, cujo layout já não é suportado, e o backup sumiu — restaure o original (verifique ou reinstale o jogo) e aplique de novo.",
                "⚠ Patch applicata con Retrofit {0}, il cui layout non è più supportato, e manca il backup — ripristina l'originale (verifica o reinstalla il gioco) e riapplica.",
                "⚠ Patché avec Retrofit {0}, dont la disposition n'est plus prise en charge, et la sauvegarde manque — restaurez l'original (vérifiez ou réinstallez le jeu), puis patchez à nouveau.",
                "⚠ Retrofit {0} でパッチ済みですが、その配置はサポート対象外になり、バックアップもありません — 元のファイルを復元（ゲームを検証／再インストール）してから再度パッチしてください。",
                "⚠ Retrofit {0}(으)로 패치되었으나 해당 배치는 더 이상 지원되지 않으며 백업도 없습니다 — 원본을 복원한 뒤(게임 검증/재설치) 다시 패치해 주세요.",
                "⚠ 由 Retrofit {0} 打过补丁，该布局已不再支持，且备份丢失 — 请先还原原版（验证或重装游戏），然后重新打补丁。",
                "⚠ Пропатчено Retrofit {0}, чья компоновка больше не поддерживается, а резервной копии нет — восстановите оригинал (проверьте или переустановите игру) и пропатчите заново." } },

            { "compat.missing", new[]{
                "No MFatigue.exe selected yet.",
                "Noch keine MFatigue.exe ausgewählt.",
                "Aún no se ha seleccionado MFatigue.exe.",
                "Nenhum MFatigue.exe selecionado ainda.",
                "Nessun MFatigue.exe selezionato.",
                "Aucun MFatigue.exe sélectionné pour l'instant.",
                "MFatigue.exe が未選択です。",
                "MFatigue.exe가 아직 선택되지 않았습니다.",
                "尚未选择 MFatigue.exe。",
                "MFatigue.exe ещё не выбран." } },

            { "compat.contact", new[]{
                "Not compatible? Contact us →",
                "Nicht kompatibel? Melde dich →",
                "¿No es compatible? Avísanos →",
                "Não compatível? Fale conosco →",
                "Non compatibile? Contattaci →",
                "Non compatible ? Contactez-nous →",
                "対応していない場合はご連絡ください →",
                "호환되지 않나요? 문의하기 →",
                "不兼容？请联系我们 →",
                "Не совместимо? Напишите нам →" } },

            // --- exe read-out area (info panel under the path box) ---
            { "info.build", new[]{
                "Build:", "Build:", "Versión:", "Versão:", "Build:", "Build :", "ビルド：", "빌드:", "版本：", "Сборка:" } },
            { "info.build.nightdive", new[]{
                "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)", "Nightdive 2021 (GOG/Steam)" } },
            { "info.build.unknown", new[]{
                "Unknown", "Unbekannt", "Desconocida", "Desconhecida", "Sconosciuta", "Inconnu", "不明", "알 수 없음", "未知", "Неизвестно" } },
            { "info.language", new[]{
                "Language:", "Sprachversion:", "Idioma:", "Idioma:", "Lingua:", "Langue :", "言語：", "언어:", "语言：", "Язык:" } },
            { "info.installed", new[]{
                "Patched:", "Gepatcht:", "Parcheado:", "Aplicado:", "Applicato:", "Patché :", "パッチ済み：", "패치됨:", "已打补丁：", "Пропатчено:" } },
            { "info.installed.none", new[]{
                "nothing", "nichts", "nada", "nada", "niente", "rien", "なし", "없음", "无", "ничего" } },
            { "info.cat.patch", new[]{
                "Base patch", "Basis-Patch", "Parche base", "Patch base", "Patch base", "Patch de base", "基本パッチ", "기본 패치", "基础补丁", "Базовый патч" } },
            { "info.cat.cheats", new[]{
                "Cheats", "Cheats", "Trucos", "Cheats", "Trucchi", "Triche", "チート", "치트", "秘籍", "Читы" } },
            { "info.cat.experimental", new[]{
                "Experimental", "Experimentell", "Experimental", "Experimental", "Sperimentale", "Expérimental", "実験的", "실험적", "实验性", "Эксперим." } },
            { "info.report", new[]{
                "Report version →", "Version melden →", "Informar versión →", "Relatar versão →", "Segnala versione →", "Signaler la version →", "バージョンを報告 →", "버전 신고 →", "报告版本 →", "Сообщить о версии →" } },
            { "variant.english", new[]{
                "English", "Englisch", "Inglés", "Inglês", "Inglese", "Anglais", "英語", "영어", "英语", "Английская" } },
            { "variant.german", new[]{
                "German language patch", "Deutscher Sprachpatch", "Parche de idioma alemán", "Patch de idioma alemão", "Patch lingua tedesca", "Patch de langue allemand", "ドイツ語化パッチ", "독일어 언어 패치", "德语语言补丁", "Немецкий языковой патч" } },
            { "variant.unknown", new[]{
                "Unknown language patch", "Unbekannter Sprachpatch", "Parche de idioma desconocido", "Patch de idioma desconhecido", "Patch lingua sconosciuta", "Patch de langue inconnu", "不明な言語パッチ", "알 수 없는 언어 패치", "未知语言补丁", "Неизвестный языковой патч" } },

            { "prof.unleashed.title", new[]{
                "Maximum",
                "Maximum",
                "Máximo",
                "Máximo",
                "Massimo",
                "Maximum",
                "最大",
                "최대",
                "最大",
                "Максимум" } },

            { "prof.unleashed.desc", new[]{
                "No practical limits except framerate. Memory pool 128 MB; unlimited combots (crew names are reused cyclically).",
                "Keine praktischen Limits außer der Framerate. Speicherpool 128 MB; unbegrenzte Combots (Crew-Namen werden zyklisch wiederverwendet).",
                "Sin límites prácticos salvo la tasa de fotogramas. Memoria 128 MB; combots ilimitados (los nombres de tripulación se reutilizan cíclicamente).",
                "Sem limites práticos além da taxa de quadros. Memória 128 MB; combots ilimitados (nomes de tripulação são reutilizados ciclicamente).",
                "Nessun limite pratico a parte il framerate. Pool di memoria 128 MB; combot illimitati (i nomi dell'equipaggio vengono riutilizzati ciclicamente).",
                "Aucune limite pratique hormis la fréquence d'images. Mémoire 128 Mo ; combots illimités (les noms d'équipage sont réutilisés cycliquement).",
                "フレームレート以外に実質的な制限はありません。メモリプール128MB、コンボット無制限（クルー名は循環して再利用）。",
                "프레임레이트 외에는 사실상 제한이 없습니다. 메모리 풀 128 MB, 콤봇 무제한(크루 이름은 순환하여 재사용).",
                "除帧数外没有实际限制。内存池 128 MB；机甲数量不限（机组名称循环复用）。",
                "Никаких практических ограничений, кроме частоты кадров. Пул памяти 128 МБ; комботы без лимита (имена экипажей используются циклически)." } },

            { "prof.balanced2x.title", new[]{
                "50 combots · 2× units",
                "50 Combots · 2× Einheiten",
                "50 combots · 2× unidades",
                "50 combots · 2× unidades",
                "50 combot · 2× unità",
                "50 combots · 2× unités",
                "コンボット50 · ユニット2倍",
                "콤봇 50 · 유닛 2×",
                "50 机甲 · 2× 单位",
                "50 комботов · 2× юнитов" } },

            { "prof.balanced2x.desc", new[]{
                "Keeps the native combot cap — it comes from the game's crew-name list, which only holds 50 names per faction. Unit budget: 2× the base game (memory pool 20 MB). Closest to the original feel.",
                "Behält das native Combot-Limit — es entsteht durch die Crew-Namensliste des Spiels, die nur 50 Namen pro Fraktion enthält. Einheiten-Budget: 2× des Grundspiels (Speicherpool 20 MB). Am nächsten am Original.",
                "Mantiene el límite nativo de combots: proviene de la lista de nombres de tripulación, que solo tiene 50 por facción. Presupuesto de unidades: 2× el juego base (memoria 20 MB). Lo más cercano al original.",
                "Mantém o limite nativo de combots — ele vem da lista de nomes de tripulação do jogo, com apenas 50 nomes por facção. Orçamento de unidades: 2× o jogo base (memória 20 MB). O mais próximo do original.",
                "Mantiene il limite nativo dei combot — deriva dalla lista dei nomi dell'equipaggio del gioco, che contiene solo 50 nomi per fazione. Budget unità: 2× il gioco base (pool di memoria 20 MB). Il più fedele all'originale.",
                "Conserve la limite native de combots — elle vient de la liste de noms d'équipage du jeu, qui ne contient que 50 noms par faction. Budget d'unités : 2× le jeu de base (mémoire 20 Mo). Au plus près de l'original.",
                "コンボット上限は原作のまま — これはゲームのクルー名リストに由来し、1勢力あたり50名しかありません。ユニット数は原作の2倍（メモリプール20MB）。最もオリジナルに近い設定です。",
                "원작의 콤봇 상한을 유지합니다 — 게임의 크루 이름 목록에 진영당 50개의 이름밖에 없어서 생기는 제한입니다. 유닛 예산: 원작의 2×(메모리 풀 20 MB). 원작의 느낌에 가장 가깝습니다.",
                "保留原版机甲上限 — 该上限源自游戏的机组名称列表，每个阵营仅有 50 个名字。单位预算：原版的 2 倍（内存池 20 MB）。最接近原版体验。",
                "Сохраняет исходный лимит комботов — он задан списком имён экипажей игры, где всего 50 имён на фракцию. Бюджет юнитов: 2× от оригинала (пул памяти 20 МБ). Ближе всего к оригиналу." } },

            { "prof.balanced4x.title", new[]{
                "50 combots · 4× units ★",
                "50 Combots · 4× Einheiten ★",
                "50 combots · 4× unidades ★",
                "50 combots · 4× unidades ★",
                "50 combot · 4× unità ★",
                "50 combots · 4× unités ★",
                "コンボット50 · ユニット4倍 ★",
                "콤봇 50 · 유닛 4× ★",
                "50 机甲 · 4× 单位 ★",
                "50 комботов · 4× юнитов ★" } },

            { "prof.balanced4x.desc", new[]{
                "Keeps the native combot cap — it comes from the game's crew-name list, which only holds 50 names per faction. Unit budget: 4× the base game (memory pool 40 MB). ★ Recommended for 6+ players.",
                "Behält das native Combot-Limit — es entsteht durch die Crew-Namensliste des Spiels, die nur 50 Namen pro Fraktion enthält. Einheiten-Budget: 4× des Grundspiels (Speicherpool 40 MB). ★ Empfohlen für 6+ Spieler.",
                "Mantiene el límite nativo de combots: proviene de la lista de nombres de tripulación, que solo tiene 50 por facción. Presupuesto de unidades: 4× el juego base (memoria 40 MB). ★ Recomendado para 6+ jugadores.",
                "Mantém o limite nativo de combots — ele vem da lista de nomes de tripulação do jogo, com apenas 50 nomes por facção. Orçamento de unidades: 4× o jogo base (memória 40 MB). ★ Recomendado para 6+ jogadores.",
                "Mantiene il limite nativo dei combot — deriva dalla lista dei nomi dell'equipaggio del gioco, che contiene solo 50 nomi per fazione. Budget unità: 4× il gioco base (pool di memoria 40 MB). ★ Consigliato per 6+ giocatori.",
                "Conserve la limite native de combots — elle vient de la liste de noms d'équipage du jeu, qui ne contient que 50 noms par faction. Budget d'unités : 4× le jeu de base (mémoire 40 Mo). ★ Recommandé pour 6+ joueurs.",
                "コンボット上限は原作のまま — これはゲームのクルー名リストに由来し、1勢力あたり50名しかありません。ユニット数は原作の4倍（メモリプール40MB）。★ 6人以上のプレイに推奨。",
                "원작의 콤봇 상한을 유지합니다 — 게임의 크루 이름 목록에 진영당 50개의 이름밖에 없어서 생기는 제한입니다. 유닛 예산: 원작의 4×(메모리 풀 40 MB). ★ 6인 이상 플레이에 권장합니다.",
                "保留原版机甲上限 — 该上限源自游戏的机组名称列表，每个阵营仅有 50 个名字。单位预算：原版的 4 倍（内存池 40 MB）。★ 推荐 6 人以上游戏。",
                "Сохраняет исходный лимит комботов — он задан списком имён экипажей игры, где всего 50 имён на фракцию. Бюджет юнитов: 4× от оригинала (пул памяти 40 МБ). ★ Рекомендуется для 6+ игроков." } },

            { "prof.balanced8x.title", new[]{
                "50 combots · 8× units",
                "50 Combots · 8× Einheiten",
                "50 combots · 8× unidades",
                "50 combots · 8× unidades",
                "50 combot · 8× unità",
                "50 combots · 8× unités",
                "コンボット50 · ユニット8倍",
                "콤봇 50 · 유닛 8×",
                "50 机甲 · 8× 单位",
                "50 комботов · 8× юнитов" } },

            { "prof.balanced8x.desc", new[]{
                "Keeps the native combot cap — it comes from the game's crew-name list, which only holds 50 names per faction. Unit budget: 8× the base game (memory pool 80 MB). For big battles with lots of vehicles.",
                "Behält das native Combot-Limit — es entsteht durch die Crew-Namensliste des Spiels, die nur 50 Namen pro Fraktion enthält. Einheiten-Budget: 8× des Grundspiels (Speicherpool 80 MB). Für große Schlachten mit vielen Fahrzeugen.",
                "Mantiene el límite nativo de combots: proviene de la lista de nombres de tripulación, que solo tiene 50 por facción. Presupuesto de unidades: 8× el juego base (memoria 80 MB). Para grandes batallas con muchos vehículos.",
                "Mantém o limite nativo de combots — ele vem da lista de nomes de tripulação do jogo, com apenas 50 nomes por facção. Orçamento de unidades: 8× o jogo base (memória 80 MB). Para grandes batalhas com muitos veículos.",
                "Mantiene il limite nativo dei combot — deriva dalla lista dei nomi dell'equipaggio del gioco, che contiene solo 50 nomi per fazione. Budget unità: 8× il gioco base (pool di memoria 80 MB). Per grandi battaglie con molti veicoli.",
                "Conserve la limite native de combots — elle vient de la liste de noms d'équipage du jeu, qui ne contient que 50 noms par faction. Budget d'unités : 8× le jeu de base (mémoire 80 Mo). Pour de grandes batailles avec beaucoup de véhicules.",
                "コンボット上限は原作のまま — これはゲームのクルー名リストに由来し、1勢力あたり50名しかありません。ユニット数は原作の8倍（メモリプール80MB）。車両が多い大規模戦闘向け。",
                "원작의 콤봇 상한을 유지합니다 — 게임의 크루 이름 목록에 진영당 50개의 이름밖에 없어서 생기는 제한입니다. 유닛 예산: 원작의 8×(메모리 풀 80 MB). 차량이 많은 대규모 전투에 적합합니다.",
                "保留原版机甲上限 — 该上限源自游戏的机组名称列表，每个阵营仅有 50 个名字。单位预算：原版的 8 倍（内存池 80 MB）。适合车辆众多的大规模战斗。",
                "Сохраняет исходный лимит комботов — он задан списком имён экипажей игры, где всего 50 имён на фракцию. Бюджет юнитов: 8× от оригинала (пул памяти 80 МБ). Для больших сражений с массой техники." } },

            { "prof.cheats.title", new[]{
                "Maximum + Cheats  (player-only)",
                "Maximum + Cheats  (player-only)",
                "Máximo + Trucos  (solo jugador)",
                "Máximo + Trapaças  (só o jogador)",
                "Massimo + Trucchi  (solo giocatore)",
                "Maximum + Triches  (joueur uniquement)",
                "最大 + チート  (プレイヤーのみ)",
                "최대 + 치트  (플레이어 전용)",
                "最大 + 作弊  (仅玩家)",
                "Максимум + читы  (только игрок)" } },

            { "prof.cheats.desc", new[]{
                "Maximum plus cheats for YOU only: free building (no metajoule/resource cost), turbo build speed, no fog of war. The AI opponents keep playing by the normal rules.",
                "Maximum plus Cheats nur für DICH: gratis bauen (keine Metajoule-/Ressourcen-Kosten), Turbo-Bau, kein Nebel des Krieges. Die KI-Gegner spielen weiter nach normalen Regeln.",
                "Máximo más trucos solo para TI: construcción gratis (sin coste de metajulios/recursos), construcción turbo, sin niebla de guerra. La IA sigue jugando con las reglas normales.",
                "Máximo mais trapaças só para VOCÊ: construção grátis (sem custo de metajoules/recursos), construção turbo, sem névoa de guerra. A IA continua jogando pelas regras normais.",
                "Massimo più trucchi solo per TE: costruzione gratuita (nessun costo di metajoule/risorse), costruzione turbo, niente nebbia di guerra. Le IA avversarie continuano a giocare secondo le regole normali.",
                "Maximum plus des triches pour VOUS uniquement : construction gratuite (sans coût en métajoules/ressources), construction turbo, sans brouillard de guerre. L'IA continue de jouer selon les règles normales.",
                "最大設定に加え、あなただけにチート：無料建設（メタジュール・資源コストなし）、高速建設、戦場の霧なし。AIは通常ルールのまま戦います。",
                "최대 설정에 더해 플레이어 본인에게만 치트가 적용됩니다: 무료 건설(메타줄·자원 소모 없음), 초고속 건설, 전쟁의 안개 없음. AI 상대는 계속 일반 규칙대로 플레이합니다.",
                "在最大设置基础上，仅对你启用作弊：免费建造（不消耗兆焦耳／资源）、极速建造、无战争迷雾。AI 对手仍按正常规则游戏。",
                "Максимум плюс читы только для ВАС: бесплатная постройка (без затрат мегаджоулей/ресурсов), турбо-строительство, без тумана войны. ИИ продолжает играть по обычным правилам." } },

            { "prof.cheats_all.title", new[]{
                "Maximum + Cheats for EVERYONE  (incl. AI)",
                "Maximum + Cheats für ALLE  (inkl. KI)",
                "Máximo + Trucos para TODOS  (incl. IA)",
                "Máximo + Trapaças para TODOS  (incl. IA)",
                "Massimo + Trucchi per TUTTI  (IA inclusa)",
                "Maximum + Triches pour TOUS  (IA incluse)",
                "最大 + 全員にチート  (AI含む)",
                "최대 + 모두에게 치트  (AI 포함)",
                "最大 + 所有人作弊  (含 AI)",
                "Максимум + читы для ВСЕХ  (вкл. ИИ)" } },

            { "prof.cheats_all.desc", new[]{
                "Maximum plus cheats for every player including the AI: free building, turbo build, no fog. Warning — this makes the AI extremely strong. For chaos testing.",
                "Maximum plus Cheats für ALLE Spieler inkl. KI: gratis bauen, Turbo-Bau, kein Nebel. Achtung — die KI wird dadurch extrem stark. Für Chaos-Tests.",
                "Máximo más trucos para todos los jugadores, incluida la IA: construcción gratis, construcción turbo, sin niebla. Aviso: esto vuelve a la IA extremadamente fuerte. Para pruebas caóticas.",
                "Máximo mais trapaças para todos os jogadores, incluindo a IA: construção grátis, construção turbo, sem névoa. Atenção — isso deixa a IA extremamente forte. Para testes caóticos.",
                "Massimo più trucchi per tutti i giocatori, IA inclusa: costruzione gratuita, costruzione turbo, niente nebbia. Attenzione — così l'IA diventa fortissima. Per test caotici.",
                "Maximum plus des triches pour tous les joueurs, IA comprise : construction gratuite, construction turbo, sans brouillard. Attention — l'IA devient extrêmement puissante. Pour des tests chaotiques.",
                "最大設定に加え、AIを含む全プレイヤーにチート：無料建設、高速建設、霧なし。警告：AIが非常に強力になります。カオステスト用。",
                "최대 설정에 더해 AI를 포함한 모든 플레이어에게 치트가 적용됩니다: 무료 건설, 초고속 건설, 전쟁의 안개 없음. 경고 — AI가 극도로 강해집니다. 카오스 테스트용.",
                "在最大设置基础上，对所有玩家（含 AI）启用作弊：免费建造、极速建造、无迷雾。警告——这会让 AI 变得极其强大。用于混乱测试。",
                "Максимум плюс читы для всех игроков, включая ИИ: бесплатная постройка, турбо-строительство, без тумана. Внимание — ИИ станет чрезвычайно сильным. Для хаотичных тестов." } },

            // --- Banner easter egg: the robot talks back ---
            { "bot.click1", new[]{
                "Hey! That tickles.",
                "Hey! Das kitzelt.",
                "¡Eh! Eso hace cosquillas.",
                "Ei! Isso faz cócegas.",
                "Ehi! Fa il solletico.",
                "Hé ! Ça chatouille.",
                "おい！くすぐったいぞ。",
                "야! 간지럽잖아.",
                "喂！好痒。",
                "Эй! Щекотно." } },

            { "bot.click2", new[]{
                "Stop clicking on me!",
                "Hör auf, auf mir rumzuklicken!",
                "¡Deja de hacerme clic!",
                "Pare de clicar em mim!",
                "Smettila di cliccare su di me!",
                "Arrête de cliquer sur moi !",
                "クリックするのはやめろ！",
                "그만 좀 클릭해!",
                "别再点我了！",
                "Хватит по мне кликать!" } },

            { "bot.click3", new[]{
                "I have a plasma cannon, you know.",
                "Ich habe eine Plasmakanone, weißt du.",
                "Tengo un cañón de plasma, ¿sabes?",
                "Eu tenho um canhão de plasma, sabia?",
                "Ho un cannone al plasma, sai.",
                "J'ai un canon à plasma, tu sais.",
                "こっちにはプラズマキャノンがあるんだぞ。",
                "나한테 플라즈마 캐논 있는 거 알지?",
                "我可是有等离子炮的。",
                "У меня, между прочим, плазменная пушка." } },

            { "bot.click4", new[]{
                "⚠ One more click and something TERRIBLE happens.",
                "⚠ Noch ein Klick und etwas FURCHTBARES passiert.",
                "⚠ Un clic más y pasará algo TERRIBLE.",
                "⚠ Mais um clique e algo TERRÍVEL vai acontecer.",
                "⚠ Ancora un clic e succederà qualcosa di TERRIBILE.",
                "⚠ Encore un clic et quelque chose de TERRIBLE arrivera.",
                "⚠ もう一度クリックしたら、恐ろしいことが起きるぞ。",
                "⚠ 한 번만 더 클릭하면 아주 끔찍한 일이 벌어진다.",
                "⚠ 再点一次就会发生可怕的事。",
                "⚠ Ещё один клик — и случится нечто УЖАСНОЕ." } },

            // GPL-3.0 §5(d) "Appropriate Legal Notices": copyright + no-warranty +
            // redistribution terms + how to view the licence. Derivative works must
            // keep displaying this — that is what enforces visible attribution.
            { "credits.legal", new[]{
                "© 2026 Dantalion (github.com/realDantalion) — free software under the GNU GPL v3, with ABSOLUTELY NO WARRANTY.",
                "© 2026 Dantalion (github.com/realDantalion) — freie Software unter der GNU GPL v3, OHNE JEDE GEWÄHRLEISTUNG.",
                "© 2026 Dantalion (github.com/realDantalion) — software libre bajo la GNU GPL v3, SIN NINGUNA GARANTÍA.",
                "© 2026 Dantalion (github.com/realDantalion) — software livre sob a GNU GPL v3, SEM QUALQUER GARANTIA.",
                "© 2026 Dantalion (github.com/realDantalion) — software libero sotto licenza GNU GPL v3, SENZA ALCUNA GARANZIA.",
                "© 2026 Dantalion (github.com/realDantalion) — logiciel libre sous GNU GPL v3, SANS AUCUNE GARANTIE.",
                "© 2026 Dantalion (github.com/realDantalion) — GNU GPL v3 のフリーソフトウェア。無保証です。",
                "© 2026 Dantalion (github.com/realDantalion) — GNU GPL v3에 따라 배포되는 자유 소프트웨어이며, 어떠한 보증도 제공하지 않습니다.",
                "© 2026 Dantalion (github.com/realDantalion) — 依据 GNU GPL v3 发布的自由软件，不提供任何担保。",
                "© 2026 Dantalion (github.com/realDantalion) — свободное ПО на условиях GNU GPL v3, БЕЗ КАКИХ-ЛИБО ГАРАНТИЙ." } },

            { "credits.license", new[]{
                "View licence (GNU GPL v3)",
                "Lizenz anzeigen (GNU GPL v3)",
                "Ver licencia (GNU GPL v3)",
                "Ver licença (GNU GPL v3)",
                "Vedi licenza (GNU GPL v3)",
                "Voir la licence (GNU GPL v3)",
                "ライセンスを表示 (GNU GPL v3)",
                "라이선스 보기 (GNU GPL v3)",
                "查看许可证 (GNU GPL v3)",
                "Показать лицензию (GNU GPL v3)" } },

            { "credits.thanks", new[]{
                "Thanks to Zono for Metal Fatigue, and to Nightdive Studios for the re-release.",
                "Dank an Zono für Metal Fatigue und an Nightdive Studios für die Neuveröffentlichung.",
                "Gracias a Zono por Metal Fatigue y a Nightdive Studios por el relanzamiento.",
                "Obrigado à Zono por Metal Fatigue e à Nightdive Studios pelo relançamento.",
                "Grazie a Zono per Metal Fatigue e a Nightdive Studios per la riedizione.",
                "Merci à Zono pour Metal Fatigue, et à Nightdive Studios pour la réédition.",
                "Metal Fatigue を制作した Zono、再リリースした Nightdive Studios に感謝します。",
                "Metal Fatigue를 만든 Zono와 재발매를 맡은 Nightdive Studios에 감사드립니다.",
                "感谢 Zono 制作 Metal Fatigue，感谢 Nightdive Studios 重新发行。",
                "Спасибо Zono за Metal Fatigue и Nightdive Studios за переиздание." } },

            { "msg.found", new[]{
                "Found: {0}",
                "Gefunden: {0}",
                "Encontrado: {0}",
                "Encontrado: {0}",
                "Trovato: {0}",
                "Trouvé : {0}",
                "検出: {0}",
                "찾음: {0}",
                "已找到：{0}",
                "Найдено: {0}" } },

            { "msg.notFound", new[]{
                "Not detected automatically — please use 'Browse…'.",
                "Nicht automatisch gefunden — bitte 'Durchsuchen…' verwenden.",
                "No se detectó automáticamente: usa «Examinar…».",
                "Não detectado automaticamente — use «Procurar…».",
                "Non rilevato automaticamente — usa «Sfoglia…».",
                "Non détecté automatiquement — utilisez « Parcourir… ».",
                "自動検出できませんでした。「参照…」をご利用ください。",
                "자동으로 찾지 못했습니다 — '찾아보기…'를 사용해 주세요.",
                "未能自动检测 — 请使用“浏览…”。",
                "Не найдено автоматически — используйте «Обзор…»." } },

            { "msg.searchError", new[]{
                "Search error: {0}",
                "Suchfehler: {0}",
                "Error de búsqueda: {0}",
                "Erro na busca: {0}",
                "Errore di ricerca: {0}",
                "Erreur de recherche : {0}",
                "検索エラー: {0}",
                "검색 오류: {0}",
                "搜索错误：{0}",
                "Ошибка поиска: {0}" } },

            { "msg.exeMissing", new[]{
                "MFatigue.exe not found. Please check the path.",
                "MFatigue.exe nicht gefunden. Bitte Pfad prüfen.",
                "No se encontró MFatigue.exe. Comprueba la ruta.",
                "MFatigue.exe não encontrado. Verifique o caminho.",
                "MFatigue.exe non trovato. Controlla il percorso.",
                "MFatigue.exe introuvable. Vérifiez le chemin.",
                "MFatigue.exe が見つかりません。パスをご確認ください。",
                "MFatigue.exe를 찾을 수 없습니다. 경로를 확인해 주세요.",
                "未找到 MFatigue.exe。请检查路径。",
                "MFatigue.exe не найден. Проверьте путь." } },

            { "msg.wrongName", new[]{
                "The target file must be named MFatigue.exe (the game won't start otherwise).",
                "Die Zieldatei muss MFatigue.exe heißen (das Spiel startet sonst nicht).",
                "El archivo debe llamarse MFatigue.exe (de lo contrario el juego no arranca).",
                "O arquivo deve se chamar MFatigue.exe (senão o jogo não inicia).",
                "Il file di destinazione deve chiamarsi MFatigue.exe (altrimenti il gioco non si avvia).",
                "Le fichier cible doit s'appeler MFatigue.exe (sinon le jeu ne démarre pas).",
                "対象ファイル名は MFatigue.exe である必要があります（そうでないとゲームが起動しません）。",
                "대상 파일의 이름은 MFatigue.exe여야 합니다(그렇지 않으면 게임이 실행되지 않습니다).",
                "目标文件必须命名为 MFatigue.exe（否则游戏无法启动）。",
                "Целевой файл должен называться MFatigue.exe (иначе игра не запустится)." } },

            { "msg.patchOk", new[]{
                "Patch applied successfully!\n\nVersion: {0}\n\nA backup (MFatigue.exe.bak) was created.",
                "Patch erfolgreich angewendet!\n\nVersion: {0}\n\nEin Backup (MFatigue.exe.bak) wurde angelegt.",
                "¡Parche aplicado correctamente!\n\nVersión: {0}\n\nSe creó una copia de seguridad (MFatigue.exe.bak).",
                "Patch aplicado com sucesso!\n\nVersão: {0}\n\nUm backup (MFatigue.exe.bak) foi criado.",
                "Patch applicata con successo!\n\nVersione: {0}\n\nÈ stato creato un backup (MFatigue.exe.bak).",
                "Patch appliqué avec succès !\n\nVersion : {0}\n\nUne sauvegarde (MFatigue.exe.bak) a été créée.",
                "パッチを適用しました！\n\nバージョン: {0}\n\nバックアップ (MFatigue.exe.bak) を作成しました。",
                "패치를 성공적으로 적용했습니다!\n\n버전: {0}\n\n백업(MFatigue.exe.bak)을 생성했습니다.",
                "补丁应用成功！\n\n版本：{0}\n\n已创建备份 (MFatigue.exe.bak)。",
                "Патч успешно применён!\n\nВерсия: {0}\n\nСоздана резервная копия (MFatigue.exe.bak)." } },

            { "msg.denied", new[]{
                "No write access to the game file.\nPlease run the patcher as administrator.",
                "Kein Schreibzugriff auf die Spieldatei.\nBitte den Patcher als Administrator ausführen.",
                "Sin acceso de escritura al archivo del juego.\nEjecuta el parcheador como administrador.",
                "Sem acesso de escrita ao arquivo do jogo.\nExecute o patcher como administrador.",
                "Nessun accesso in scrittura al file del gioco.\nEsegui il patcher come amministratore.",
                "Pas d'accès en écriture au fichier du jeu.\nLancez le patcheur en tant qu'administrateur.",
                "ゲームファイルへの書き込み権限がありません。\n管理者としてパッチャーを実行してください。",
                "게임 파일에 쓰기 권한이 없습니다.\n패처를 관리자 권한으로 실행해 주세요.",
                "无法写入游戏文件。\n请以管理员身份运行本补丁程序。",
                "Нет доступа на запись к файлу игры.\nЗапустите патчер от имени администратора." } },

            { "msg.restored", new[]{
                "Original restored.",
                "Original wiederhergestellt.",
                "Original restaurado.",
                "Original restaurado.",
                "Originale ripristinato.",
                "Original restauré.",
                "オリジナルに戻しました。",
                "원본을 복원했습니다.",
                "已还原原版。",
                "Оригинал восстановлен." } },

            { "msg.unlocked", new[]{
                "*** Cheat mode unlocked — 2 extra versions available. ***",
                "*** Cheat-Modus freigeschaltet — 2 zusätzliche Versionen verfügbar. ***",
                "*** Modo trucos desbloqueado: 2 versiones adicionales disponibles. ***",
                "*** Modo trapaça desbloqueado — 2 versões extras disponíveis. ***",
                "*** Modalità trucchi sbloccata — 2 versioni extra disponibili. ***",
                "*** Mode triche débloqué — 2 versions supplémentaires disponibles. ***",
                "*** チートモード解除 — 追加バージョン2種が利用可能です。 ***",
                "*** 치트 모드 잠금 해제 — 추가 버전 2개 사용 가능. ***",
                "*** 作弊模式已解锁 — 新增 2 个版本。 ***",
                "*** Режим читов разблокирован — доступны 2 доп. версии. ***" } },

            { "ttl.done", new[]{
                "Done",
                "Fertig",
                "Listo",
                "Concluído",
                "Fatto",
                "Terminé",
                "完了",
                "완료",
                "完成",
                "Готово" } },

            { "ttl.error", new[]{
                "Error",
                "Fehler",
                "Error",
                "Erro",
                "Errore",
                "Erreur",
                "エラー",
                "오류",
                "错误",
                "Ошибка" } },

            { "ttl.denied", new[]{
                "Access denied",
                "Zugriff verweigert",
                "Acceso denegado",
                "Acesso negado",
                "Accesso negato",
                "Accès refusé",
                "アクセス拒否",
                "액세스 거부됨",
                "访问被拒绝",
                "Доступ запрещён" } },

            { "log.backup", new[]{
                "Backup created: {0}",
                "Backup erstellt: {0}",
                "Copia de seguridad creada: {0}",
                "Backup criado: {0}",
                "Backup creato: {0}",
                "Sauvegarde créée : {0}",
                "バックアップ作成: {0}",
                "백업 생성됨: {0}",
                "已创建备份：{0}",
                "Создана резервная копия: {0}" } },

            { "log.applying", new[]{
                "Applying version: {0}",
                "Wende Version an: {0}",
                "Aplicando versión: {0}",
                "Aplicando versão: {0}",
                "Applicazione della versione: {0}",
                "Application de la version : {0}",
                "適用中のバージョン: {0}",
                "버전 적용 중: {0}",
                "正在应用版本：{0}",
                "Применяется версия: {0}" } },

            { "log.patched", new[]{
                "  patched: {0} @ 0x{1:X}",
                "  gepatcht: {0} @ 0x{1:X}",
                "  parcheado: {0} @ 0x{1:X}",
                "  aplicado: {0} @ 0x{1:X}",
                "  patchato: {0} @ 0x{1:X}",
                "  patché : {0} @ 0x{1:X}",
                "  適用: {0} @ 0x{1:X}",
                "  패치됨: {0} @ 0x{1:X}",
                "  已修补：{0} @ 0x{1:X}",
                "  пропатчено: {0} @ 0x{1:X}" } },

            { "log.verified", new[]{
                "Verification OK. Patch complete.",
                "Verifizierung OK. Patch abgeschlossen.",
                "Verificación correcta. Parche completado.",
                "Verificação OK. Patch concluído.",
                "Verifica OK. Patch completata.",
                "Vérification OK. Patch terminé.",
                "検証OK。パッチ完了。",
                "검증 성공. 패치 완료.",
                "校验通过。补丁完成。",
                "Проверка пройдена. Патч завершён." } },

            { "log.restored", new[]{
                "Original restored from backup.",
                "Original aus Backup wiederhergestellt.",
                "Original restaurado desde la copia de seguridad.",
                "Original restaurado a partir do backup.",
                "Originale ripristinato dal backup.",
                "Original restauré depuis la sauvegarde.",
                "バックアップからオリジナルを復元しました。",
                "백업에서 원본을 복원했습니다.",
                "已从备份还原原版。",
                "Оригинал восстановлен из резервной копии." } },

            { "log.error", new[]{
                "ERROR: {0}",
                "FEHLER: {0}",
                "ERROR: {0}",
                "ERRO: {0}",
                "ERRORE: {0}",
                "ERREUR : {0}",
                "エラー: {0}",
                "오류: {0}",
                "错误：{0}",
                "ОШИБКА: {0}" } },

            { "err.notFound", new[]{
                "MFatigue.exe not found.",
                "MFatigue.exe nicht gefunden.",
                "No se encontró MFatigue.exe.",
                "MFatigue.exe não encontrado.",
                "MFatigue.exe non trovato.",
                "MFatigue.exe introuvable.",
                "MFatigue.exe が見つかりません。",
                "MFatigue.exe를 찾을 수 없습니다.",
                "未找到 MFatigue.exe。",
                "MFatigue.exe не найден." } },

            { "err.notPristine", new[]{
                "MFatigue.exe does not look like an unmodified build and there is no backup (.bak).\nIt may already be patched or be a different build. Verify/reinstall the game and try again.",
                "MFatigue.exe sieht nicht wie eine unveränderte Version aus und es gibt kein Backup (.bak).\nSie ist evtl. schon gepatcht oder ein anderer Build. Spiel verifizieren/neu installieren und erneut versuchen.",
                "MFatigue.exe no parece una versión sin modificar y no hay copia de seguridad (.bak).\nPuede que ya esté parcheada o sea otra compilación. Verifica/reinstala el juego e inténtalo de nuevo.",
                "MFatigue.exe não parece uma versão original e não há backup (.bak).\nPode já estar aplicado ou ser outra versão. Verifique/reinstale o jogo e tente de novo.",
                "MFatigue.exe non sembra una build originale e non esiste alcun backup (.bak).\nPotrebbe essere già patchata o trattarsi di una build diversa. Verifica/reinstalla il gioco e riprova.",
                "MFatigue.exe ne semble pas être une version d'origine et aucune sauvegarde (.bak) n'existe.\nElle est peut-être déjà patchée ou d'une autre version. Vérifiez/réinstallez le jeu et réessayez.",
                "MFatigue.exe が未改変のビルドではないようで、バックアップ (.bak) もありません。\n既にパッチ済みか、別のビルドの可能性があります。ゲームを検証・再インストールしてお試しください。",
                "MFatigue.exe가 수정되지 않은 빌드가 아닌 것으로 보이며, 백업(.bak)도 없습니다.\n이미 패치되었거나 다른 빌드일 수 있습니다. 게임 파일을 검증하거나 재설치한 후 다시 시도해 주세요.",
                "MFatigue.exe 似乎不是未修改的版本，且没有备份 (.bak)。\n它可能已被打过补丁或是其他版本。请验证／重装游戏后重试。",
                "MFatigue.exe не похож на неизменённую сборку, и резервной копии (.bak) нет.\nВозможно, он уже пропатчен или это другая сборка. Проверьте/переустановите игру и повторите." } },

            { "err.badBackup", new[]{
                "The backup (.bak) is not a clean MFatigue.exe. Please delete it and restore an unmodified copy.",
                "Das Backup (.bak) ist keine saubere MFatigue.exe. Bitte löschen und eine unveränderte Kopie wiederherstellen.",
                "La copia de seguridad (.bak) no es una MFatigue.exe limpia. Elimínala y restaura una copia sin modificar.",
                "O backup (.bak) não é um MFatigue.exe limpo. Exclua-o e restaure uma cópia original.",
                "Il backup (.bak) non è un MFatigue.exe integro. Eliminalo e ripristina una copia non modificata.",
                "La sauvegarde (.bak) n'est pas une MFatigue.exe intacte. Supprimez-la et restaurez une copie d'origine.",
                "バックアップ (.bak) がクリーンな MFatigue.exe ではありません。削除して未改変のコピーを復元してください。",
                "백업(.bak)이 수정되지 않은 MFatigue.exe가 아닙니다. 삭제한 후 수정되지 않은 사본으로 복원해 주세요.",
                "备份 (.bak) 不是干净的 MFatigue.exe。请删除它并还原一份未修改的副本。",
                "Резервная копия (.bak) — не чистый MFatigue.exe. Удалите её и восстановите неизменённую копию." } },

            { "err.outOfRange", new[]{
                "Patch site {0} is outside the file.",
                "Patch-Stelle {0} liegt außerhalb der Datei.",
                "La posición de parche {0} está fuera del archivo.",
                "A posição de patch {0} está fora do arquivo.",
                "La posizione di patch {0} è fuori dal file.",
                "L'emplacement de patch {0} est hors du fichier.",
                "パッチ位置 {0} がファイル範囲外です。",
                "패치 위치 {0}이(가) 파일 범위를 벗어났습니다.",
                "补丁位置 {0} 超出文件范围。",
                "Позиция патча {0} вне файла." } },

            { "err.unexpected", new[]{
                "Unexpected bytes at {0} (0x{1:X}). Wrong build?",
                "Unerwartete Bytes bei {0} (0x{1:X}). Falscher Build?",
                "Bytes inesperados en {0} (0x{1:X}). ¿Compilación incorrecta?",
                "Bytes inesperados em {0} (0x{1:X}). Versão errada?",
                "Byte inattesi in {0} (0x{1:X}). Build errata?",
                "Octets inattendus à {0} (0x{1:X}). Mauvaise version ?",
                "{0} (0x{1:X}) に予期しないバイトがあります。ビルドが異なる可能性があります。",
                "{0} (0x{1:X})에 예기치 않은 바이트가 있습니다. 빌드가 다를 수 있습니다.",
                "在 {0} (0x{1:X}) 处发现意外字节。版本不对？",
                "Неожиданные байты в {0} (0x{1:X}). Другая сборка?" } },

            { "err.verify", new[]{
                "Verification failed at {0}.",
                "Verifizierung fehlgeschlagen bei {0}.",
                "Falló la verificación en {0}.",
                "Falha na verificação em {0}.",
                "Verifica fallita in {0}.",
                "Échec de la vérification à {0}.",
                "{0} で検証に失敗しました。",
                "{0}에서 검증에 실패했습니다.",
                "在 {0} 处校验失败。",
                "Проверка не пройдена в {0}." } },

            { "err.noBackup", new[]{
                "No backup (.bak) found to restore from.",
                "Kein Backup (.bak) zum Wiederherstellen gefunden.",
                "No se encontró copia de seguridad (.bak) para restaurar.",
                "Nenhum backup (.bak) encontrado para restaurar.",
                "Nessun backup (.bak) trovato da cui ripristinare.",
                "Aucune sauvegarde (.bak) trouvée pour la restauration.",
                "復元用のバックアップ (.bak) が見つかりません。",
                "복원할 백업(.bak)을 찾을 수 없습니다.",
                "未找到可用于还原的备份 (.bak)。",
                "Резервная копия (.bak) для восстановления не найдена." } },

            // --- 2.0 cheat tab ---
            { "tab.patch", new[]{
                "Patch", "Patch", "Parche", "Patch", "Patch", "Patch", "パッチ", "패치", "补丁", "Патч" } },
            { "tab.cheats", new[]{
                "Cheats", "Cheats", "Trucos", "Cheats", "Trucchi", "Triche", "チート", "치트", "秘籍", "Читы" } },
            { "tab.experimental", new[]{
                "Experimental", "Experimentell", "Experimental", "Experimental", "Sperimentale", "Expérimental", "実験的", "실험적", "实验性", "Эксперим." } },
            { "grp.expwarn", new[]{
                "Please read first", "Bitte zuerst lesen", "Léelo primero", "Leia primeiro", "Leggi prima", "À lire d'abord", "最初にお読みください", "먼저 읽어 주세요", "请先阅读", "Прочтите сначала" } },
            { "exp.warning", new[]{
                "⚠ These features change core game behaviour and can break saved games or multiplayer. Back up your save first and use them at your own risk.",
                "⚠ Diese Features ändern grundlegendes Spielverhalten und können Spielstände oder den Mehrspielermodus beschädigen. Mach vorher ein Backup und nutze sie auf eigene Gefahr.",
                "⚠ Estas funciones cambian el comportamiento básico del juego y pueden dañar partidas guardadas o el multijugador. Haz una copia de seguridad y úsalas bajo tu propio riesgo.",
                "⚠ Estes recursos alteram o comportamento básico do jogo e podem quebrar jogos salvos ou o multijogador. Faça backup antes e use por sua conta e risco.",
                "⚠ Queste funzioni cambiano il comportamento di base del gioco e possono rovinare i salvataggi o il multiplayer. Fai un backup e usale a tuo rischio.",
                "⚠ Ces fonctions modifient le comportement de base du jeu et peuvent casser les sauvegardes ou le multijoueur. Sauvegardez d'abord et utilisez-les à vos risques.",
                "⚠ これらの機能はゲームの基本動作を変更し、セーブデータやマルチプレイを壊す可能性があります。事前にバックアップし、自己責任でご利用ください。",
                "⚠ 이 기능들은 게임의 핵심 동작을 바꾸며 저장 게임이나 멀티플레이를 손상시킬 수 있습니다. 먼저 백업하고 본인 책임하에 사용하세요.",
                "⚠ 这些功能会改变游戏的核心行为，可能损坏存档或多人游戏。请先备份，风险自负。",
                "⚠ Эти функции меняют базовое поведение игры и могут повредить сохранения или мультиплеер. Сделайте резервную копию и используйте на свой риск." } },
            { "grp.expsoon", new[]{
                "Features", "Funktionen", "Funciones", "Recursos", "Funzioni", "Fonctions", "機能", "기능", "功能", "Функции" } },
            { "exp.soon", new[]{
                "More experimental features will follow in later versions.",
                "Weitere experimentelle Features folgen in späteren Versionen.",
                "Más funciones experimentales llegarán en versiones posteriores.",
                "Mais recursos experimentais virão em versões futuras.",
                "Altre funzioni sperimentali arriveranno nelle versioni successive.",
                "D'autres fonctions expérimentales suivront dans les prochaines versions.",
                "実験的機能は今後のバージョンでさらに追加されます。",
                "추가 실험적 기능은 향후 버전에서 제공됩니다.",
                "更多实验性功能将在后续版本中加入。",
                "Другие экспериментальные функции появятся в следующих версиях." } },
            { "grp.expspeed", new[]{
                "Unit movement speed", "Bewegungsgeschwindigkeit der Einheiten", "Velocidad de movimiento de las unidades", "Velocidade de movimento das unidades", "Velocità di movimento delle unità", "Vitesse de déplacement des unités", "ユニットの移動速度", "유닛 이동 속도", "单位移动速度", "Скорость передвижения юнитов" } },
            { "exp.speed", new[]{
                "Speed up unit movement", "Einheiten schneller bewegen", "Acelerar el movimiento de las unidades", "Acelerar o movimento das unidades", "Accelera il movimento delle unità", "Accélérer le déplacement des unités", "ユニットの移動を速くする", "유닛 이동 속도 높이기", "加快单位移动", "Ускорить передвижение юнитов" } },
            { "exp.speed.factor", new[]{
                "Factor:", "Faktor:", "Factor:", "Fator:", "Fattore:", "Facteur :", "倍率：", "배율:", "倍率：", "Множитель:" } },
            { "exp.speed.note", new[]{
                "Multiplies every unit's top speed, so faster legs still beat slower ones. Braking and arrival scale with it, so units keep stopping on target — but at very high factors they get hard to control.",
                "Multipliziert die Höchstgeschwindigkeit jeder Einheit — schnellere Beine bleiben also schneller als langsame. Bremsen und Ankommen skalieren mit, Einheiten halten weiterhin auf dem Ziel — bei sehr hohen Faktoren werden sie aber schwer steuerbar.",
                "Multiplica la velocidad máxima de cada unidad, así que las piernas rápidas siguen siendo mejores. El frenado y la llegada escalan con ella, por lo que las unidades siguen parando en el objetivo, pero con factores muy altos cuesta controlarlas.",
                "Multiplica a velocidade máxima de cada unidade, então pernas mais rápidas continuam melhores. A frenagem e a chegada acompanham, então as unidades continuam parando no alvo — mas com fatores muito altos ficam difíceis de controlar.",
                "Moltiplica la velocità massima di ogni unità, quindi le gambe veloci restano migliori. Frenata e arrivo scalano di conseguenza, così le unità si fermano ancora sul bersaglio, ma con fattori molto alti diventano difficili da controllare.",
                "Multiplie la vitesse maximale de chaque unité : les jambes rapides restent meilleures. Le freinage et l'arrivée suivent, les unités s'arrêtent donc toujours sur la cible — mais à facteur très élevé, elles deviennent difficiles à contrôler.",
                "各ユニットの最高速度に倍率を掛けます。速い脚部は速いままです。減速と到着判定も一緒にスケールするため目標地点で停止できますが、倍率が高すぎると操作が難しくなります。",
                "모든 유닛의 최고 속도에 배율을 곱하므로 빠른 다리는 여전히 더 빠릅니다. 감속과 도착 판정도 함께 조정되어 목표 지점에 정확히 멈추지만, 배율이 너무 높으면 조종이 어려워집니다.",
                "按倍率提高每个单位的最高速度，因此快速腿部依然更快。减速与到达判定会同步缩放，单位仍能停在目标点——但倍率过高会难以操控。",
                "Умножает максимальную скорость каждого юнита, поэтому быстрые ноги остаются быстрее медленных. Торможение и прибытие масштабируются вместе с ней, так что юниты по-прежнему останавливаются на цели, но при очень больших множителях ими трудно управлять." } },
            { "exp.speed.example", new[]{
                "At {0}×: Hovertruck 28 → {1}, combot with HTH legs 15.5 → {2}.",
                "Bei {0}×: Hovertruck 28 → {1}, Combot mit HTH-Beinen 15.5 → {2}.",
                "Con {0}×: Hovertruck 28 → {1}, combot con piernas HTH 15.5 → {2}.",
                "Com {0}×: Hovertruck 28 → {1}, combot com pernas HTH 15.5 → {2}.",
                "A {0}×: Hovertruck 28 → {1}, combot con gambe HTH 15.5 → {2}.",
                "À {0}× : Hovertruck 28 → {1}, combot avec jambes HTH 15.5 → {2}.",
                "{0}×：ホバートラック 28 → {1}、HTH脚部のコンボット 15.5 → {2}。",
                "{0}×: 호버트럭 28 → {1}, HTH 다리 콤봇 15.5 → {2}.",
                "{0}× 时：气垫卡车 28 → {1}，装 HTH 腿部的 Combot 15.5 → {2}。",
                "При {0}×: ховертрак 28 → {1}, комбот с ногами HTH 15.5 → {2}." } },
            { "btn.ok", new[]{
                "OK", "OK", "Aceptar", "OK", "OK", "OK", "OK", "확인", "确定", "ОК" } },
            { "btn.cancel", new[]{
                "Cancel", "Abbrechen", "Cancelar", "Cancelar", "Annulla", "Annuler", "キャンセル", "취소", "取消", "Отмена" } },

            // --- Music tab: restore Rimtech's missing CD 1 soundtrack -----------------------------
            { "tab.music", new[]{
                "Music", "Musik", "Música", "Música", "Musica", "Musique", "音楽", "음악", "音乐", "Музыка" } },
            { "banner.musicSub", new[]{
                "Bring back Rimtech's missing soundtrack",
                "Rimtechs fehlenden Soundtrack zurückholen",
                "Recupera la banda sonora perdida de Rimtech",
                "Traga de volta a trilha sonora perdida da Rimtech",
                "Riporta la colonna sonora mancante di Rimtech",
                "Récupérez la bande-son manquante de Rimtech",
                "Rimtech の失われたサウンドトラックを復活",
                "Rimtech의 사라진 사운드트랙 되살리기",
                "找回 Rimtech 缺失的原声音乐",
                "Верните утраченный саундтрек Rimtech" } },
            { "music.intro", new[]{
                "Rimtech's music was on CD 1 and is missing from the re-release, so Rimtech plays Mil-Agro's tracks. The patch gives Rimtech track numbers of its own and copies your files into them. It ships no game audio — supply the ten tracks yourself, as OGG Vorbis, ideally in CD order. File names do not matter.",
                "Rimtechs Musik lag auf CD 1 und fehlt im Re-Release, deshalb spielt Rimtech die Stücke von Mil-Agro. Der Patch gibt Rimtech eigene Tracknummern und legt deine Dateien dort ab. Spielmusik liefert er keine mit — die zehn Titel bringst du selbst mit, als OGG Vorbis, möglichst in CD-Reihenfolge. Dateinamen sind egal.",
                "La música de Rimtech estaba en el CD 1 y falta en la reedición, así que Rimtech suena con los temas de Mil-Agro. El parche da a Rimtech números de pista propios y copia ahí tus archivos. No incluye audio del juego: aporta tú las diez pistas, en OGG Vorbis y a ser posible en el orden del CD. Los nombres dan igual.",
                "A música da Rimtech estava no CD 1 e falta no relançamento, então a Rimtech toca as faixas da Mil-Agro. O patch dá à Rimtech números de faixa próprios e copia seus arquivos para lá. Não traz áudio do jogo — forneça você as dez faixas, em OGG Vorbis e de preferência na ordem do CD. Os nomes não importam.",
                "La musica della Rimtech era sul CD 1 e manca nella riedizione, così la Rimtech suona i brani della Mil-Agro. La patch assegna alla Rimtech numeri di traccia propri e vi copia i tuoi file. Non include audio del gioco: le dieci tracce portale tu, in OGG Vorbis e possibilmente nell'ordine del CD. I nomi non contano.",
                "La musique de Rimtech était sur le CD 1 et manque à la réédition : Rimtech joue donc les morceaux de Mil-Agro. Le patch attribue à Rimtech ses propres numéros de piste et y copie vos fichiers. Il ne fournit aucun audio du jeu — apportez les dix pistes vous-même, en OGG Vorbis et si possible dans l'ordre du CD. Les noms importent peu.",
                "Rimtech の音楽は CD 1 にあり、リマスター版には含まれていないため Mil-Agro の曲が流れます。このパッチは Rimtech 専用のトラック番号を割り当て、そこへあなたのファイルを配置します。ゲーム音源は同梱しません。10 曲はご自身でご用意ください（OGG Vorbis 形式、できれば CD の並び順で）。ファイル名は問いません。",
                "Rimtech의 음악은 CD 1에 있었고 재출시판에는 없어 Mil-Agro의 곡이 재생됩니다. 이 패치는 Rimtech에 전용 트랙 번호를 부여하고 그 자리에 파일을 복사합니다. 게임 오디오는 포함하지 않습니다 — 10개 트랙을 직접 준비해 주세요(OGG Vorbis, 가능하면 CD 순서대로). 파일 이름은 상관없습니다.",
                "Rimtech 的音乐在 CD 1 上，重制版未收录，因此播放的是 Mil-Agro 的曲目。本补丁为 Rimtech 分配专属的曲目编号，并把你的文件放到那里。它不附带游戏音频 — 十首曲子请自行准备，需为 OGG Vorbis，最好保持光盘顺序。文件名无所谓。",
                "Музыка Rimtech была на CD 1 и в переиздание не попала, поэтому играют треки Mil-Agro. Патч выделяет Rimtech собственные номера треков и копирует туда ваши файлы. Игрового аудио он не содержит — десять треков принесите свои, в формате OGG Vorbis и желательно в порядке диска. Имена файлов не важны." } },
            { "music.pickFolder", new[]{
                "Choose folder…", "Ordner wählen…", "Elegir carpeta…", "Escolher pasta…", "Scegli cartella…", "Choisir un dossier…", "フォルダーを選択…", "폴더 선택…", "选择文件夹…", "Выбрать папку…" } },
            { "music.pickZip", new[]{
                "Choose zip…", "Zip wählen…", "Elegir zip…", "Escolher zip…", "Scegli zip…", "Choisir un zip…", "ZIP を選択…", "ZIP 선택…", "选择 ZIP…", "Выбрать ZIP…" } },
            { "music.remove", new[]{
                "Remove imported music", "Importierte Musik entfernen", "Quitar la música importada", "Remover a música importada", "Rimuovi la musica importata", "Retirer la musique importée", "取り込んだ音楽を削除", "가져온 음악 제거", "移除已导入的音乐", "Удалить импортированную музыку" } },
            { "music.status.installed", new[]{
                "Rimtech music is installed.", "Rimtech-Musik ist installiert.", "La música de Rimtech está instalada.", "A música da Rimtech está instalada.", "La musica Rimtech è installata.", "La musique Rimtech est installée.", "Rimtech の音楽が導入済みです。", "Rimtech 음악이 설치되어 있습니다.", "Rimtech 音乐已安装。", "Музыка Rimtech установлена." } },
            { "music.status.none", new[]{
                "No Rimtech music installed yet.", "Noch keine Rimtech-Musik installiert.", "Aún no hay música de Rimtech instalada.", "Ainda sem música da Rimtech instalada.", "Nessuna musica Rimtech installata.", "Aucune musique Rimtech installée.", "Rimtech の音楽は未導入です。", "아직 Rimtech 음악이 없습니다.", "尚未安装 Rimtech 音乐。", "Музыка Rimtech ещё не установлена." } },
            { "music.review", new[]{
                "Check order…", "Reihenfolge prüfen…", "Comprobar orden…", "Conferir ordem…", "Controlla ordine…", "Vérifier l'ordre…", "順番を確認…", "순서 확인…", "检查顺序…", "Проверить порядок…" } },
            { "music.status.ready", new[]{
                "Tracks are in place — press Patch to activate them.",
                "Titel liegen bereit — zum Aktivieren auf Patch drücken.",
                "Las pistas están listas: pulsa Parchear para activarlas.",
                "As faixas estão prontas — clique em Aplicar para ativá-las.",
                "I brani sono pronti: premi Applica per attivarli.",
                "Les pistes sont en place — cliquez sur Patcher pour les activer.",
                "曲の配置が完了しました — 有効にするにはパッチを実行してください。",
                "트랙이 준비되었습니다 — 활성화하려면 패치를 누르세요.",
                "曲目已就位 — 点击打补丁以启用。",
                "Треки на месте — нажмите «Патч», чтобы их включить." } },
            { "music.status.check", new[]{
                "{0} track(s) do not match their slot's length.",
                "Bei {0} Titel(n) passt die Länge nicht zum Platz.",
                "{0} pista(s) no coinciden con la duración de su posición.",
                "{0} faixa(s) não batem com a duração da posição.",
                "{0} brano/i non corrispondono alla durata della posizione.",
                "{0} piste(s) ne correspondent pas à la durée de leur emplacement.",
                "{0} 曲が枠の長さと一致しません。",
                "{0}개 트랙의 길이가 자리와 맞지 않습니다.",
                "有 {0} 首曲目与其位置的时长不符。",
                "{0} трек(ов) не совпадают с длительностью слота." } },
            { "music.status.orphan", new[]{
                "The game is set to play imported music, but the files are gone — import them again, or press Patch to undo it.",
                "Das Spiel ist auf importierte Musik eingestellt, die Dateien fehlen aber — erneut importieren oder mit Patch rückgängig machen.",
                "El juego está configurado para la música importada, pero los archivos no están: vuelve a importarlos o pulsa Parchear para deshacerlo.",
                "O jogo está configurado para a música importada, mas os arquivos sumiram — importe de novo ou clique em Aplicar para desfazer.",
                "Il gioco è impostato sulla musica importata, ma i file non ci sono: reimportali o premi Applica per annullare.",
                "Le jeu est réglé sur la musique importée, mais les fichiers ont disparu — réimportez-les ou cliquez sur Patcher pour annuler.",
                "取り込んだ音楽を再生する設定ですが、ファイルがありません。取り込み直すか、パッチで元に戻してください。",
                "가져온 음악을 재생하도록 설정되어 있지만 파일이 없습니다 — 다시 가져오거나 패치로 되돌리세요.",
                "游戏已设置为播放导入的音乐，但文件不见了 — 请重新导入，或点击打补丁撤销。",
                "Игра настроена на импортированную музыку, но файлов нет — импортируйте заново или нажмите «Патч», чтобы отменить." } },
            { "music.err.noSource", new[]{
                "That folder or file could not be opened.", "Dieser Ordner bzw. diese Datei ließ sich nicht öffnen.", "No se pudo abrir esa carpeta o archivo.", "Não foi possível abrir essa pasta ou arquivo.", "Impossibile aprire quella cartella o quel file.", "Ce dossier ou fichier n'a pas pu être ouvert.", "そのフォルダーまたはファイルを開けませんでした。", "해당 폴더 또는 파일을 열 수 없습니다.", "无法打开该文件夹或文件。", "Не удалось открыть эту папку или файл." } },
            { "music.err.read", new[]{
                "The files could not be read.", "Die Dateien ließen sich nicht lesen.", "No se pudieron leer los archivos.", "Não foi possível ler os arquivos.", "Impossibile leggere i file.", "Les fichiers n'ont pas pu être lus.", "ファイルを読み取れませんでした。", "파일을 읽을 수 없습니다.", "无法读取这些文件。", "Не удалось прочитать файлы." } },
            { "music.err.noOgg", new[]{
                "No OGG files in there.", "Dort sind keine OGG-Dateien.", "Ahí no hay archivos OGG.", "Não há arquivos OGG aí.", "Nessun file OGG lì dentro.", "Aucun fichier OGG là-dedans.", "OGG ファイルが見つかりません。", "OGG 파일이 없습니다.", "其中没有 OGG 文件。", "Там нет файлов OGG." } },
            { "music.err.wrongFormat", new[]{
                "Found {0} audio files, but no OGG. Metal Fatigue reads its music only as OGG Vorbis — that is built into the game's audio layer and cannot be changed. Convert your files to OGG with any free converter and import them again. Keeping their original order helps; the names do not matter.",
                "{0} Audiodateien gefunden, aber keine OGG. Metal Fatigue liest Musik ausschließlich als OGG Vorbis — das steckt in der Audioschicht des Spiels und lässt sich nicht ändern. Wandle die Dateien mit einem beliebigen kostenlosen Konverter in OGG um und importiere sie erneut. Die ursprüngliche Reihenfolge beizubehalten hilft; die Namen sind egal.",
                "Se encontraron {0} archivos de audio, pero ningún OGG. Metal Fatigue solo lee música en OGG Vorbis: está integrado en la capa de audio del juego y no se puede cambiar. Convierte tus archivos a OGG con cualquier conversor gratuito e impórtalos de nuevo. Mantén el orden original; los nombres dan igual.",
                "Encontrados {0} arquivos de áudio, mas nenhum OGG. O Metal Fatigue lê música apenas em OGG Vorbis — isso está embutido na camada de áudio do jogo e não pode ser mudado. Converta seus arquivos para OGG com qualquer conversor gratuito e importe novamente. Mantenha a ordem original; os nomes não importam.",
                "Trovati {0} file audio, ma nessun OGG. Metal Fatigue legge la musica solo come OGG Vorbis: è integrato nel livello audio del gioco e non è modificabile. Converti i file in OGG con un qualsiasi convertitore gratuito e reimportali. Mantieni l'ordine originale; i nomi non contano.",
                "{0} fichiers audio trouvés, mais aucun OGG. Metal Fatigue ne lit sa musique qu'en OGG Vorbis — c'est intégré à la couche audio du jeu et non modifiable. Convertissez vos fichiers en OGG avec n'importe quel convertisseur gratuit, puis réimportez-les. Conservez l'ordre d'origine ; les noms importent peu.",
                "音声ファイルを {0} 件見つけましたが、OGG がありません。Metal Fatigue は音楽を OGG Vorbis でしか読み込めません（ゲームの音声処理に組み込まれており変更できません）。無料の変換ソフトで OGG に変換して取り込み直してください。元の順番は保ってください。名前は問いません。",
                "오디오 파일 {0}개를 찾았지만 OGG가 없습니다. Metal Fatigue는 음악을 OGG Vorbis로만 읽습니다 — 게임의 오디오 계층에 내장되어 있어 바꿀 수 없습니다. 무료 변환 프로그램으로 OGG로 변환한 뒤 다시 가져오세요. 원래 순서는 유지하세요. 이름은 상관없습니다.",
                "找到 {0} 个音频文件，但没有 OGG。Metal Fatigue 只能读取 OGG Vorbis 格式的音乐 — 这内建于游戏的音频层，无法更改。请用任意免费转换工具转成 OGG 后重新导入。请保持原有顺序；文件名无所谓。",
                "Найдено аудиофайлов: {0}, но ни одного OGG. Metal Fatigue читает музыку только в формате OGG Vorbis — это заложено в аудиослое игры и не меняется. Преобразуйте файлы в OGG любым бесплатным конвертером и импортируйте снова. Сохраните исходный порядок; имена не важны." } },
            { "music.err.count", new[]{
                "Expected exactly 10 OGG files, found {0}.", "Erwartet werden genau 10 OGG-Dateien, gefunden wurden {0}.", "Se esperaban exactamente 10 archivos OGG, se encontraron {0}.", "Esperados exatamente 10 arquivos OGG, encontrados {0}.", "Attesi esattamente 10 file OGG, trovati {0}.", "Exactement 10 fichiers OGG sont attendus, {0} trouvés.", "OGG ファイルはちょうど 10 件必要ですが、{0} 件でした。", "OGG 파일이 정확히 10개여야 하는데 {0}개입니다.", "需要正好 10 个 OGG 文件，实际找到 {0} 个。", "Нужно ровно 10 файлов OGG, найдено {0}." } },
            { "music.legend.active", new[]{
                "in use by the game", "wird vom Spiel benutzt", "en uso por el juego", "em uso pelo jogo", "in uso dal gioco", "utilisé par le jeu", "ゲームで使用中", "게임에서 사용 중", "游戏正在使用", "используется игрой" } },
            { "music.legend.pending", new[]{
                "copied, not patched yet", "kopiert, noch nicht gepatcht", "copiado, aún sin parchear", "copiado, ainda não aplicado", "copiato, non ancora applicato", "copié, pas encore patché", "コピー済み・未パッチ", "복사됨, 아직 패치 안 함", "已复制，尚未打补丁", "скопировано, ещё не пропатчено" } },
            { "music.legend.mismatch", new[]{
                "length does not fit this slot", "Länge passt nicht zum Platz", "la duración no encaja aquí", "a duração não bate com a posição", "la durata non corrisponde", "la durée ne correspond pas", "長さが枠と合わない", "길이가 자리와 맞지 않음", "时长与位置不符", "длительность не подходит" } },
            { "music.err.locked", new[]{
                "These files are in use and could not be deleted: {0}. Close the game and try again.",
                "Diese Dateien sind in Benutzung und ließen sich nicht löschen: {0}. Schließe das Spiel und versuche es erneut.",
                "Estos archivos están en uso y no se pudieron borrar: {0}. Cierra el juego e inténtalo de nuevo.",
                "Estes arquivos estão em uso e não puderam ser excluídos: {0}. Feche o jogo e tente de novo.",
                "Questi file sono in uso e non è stato possibile eliminarli: {0}. Chiudi il gioco e riprova.",
                "Ces fichiers sont utilisés et n'ont pas pu être supprimés : {0}. Fermez le jeu et réessayez.",
                "次のファイルは使用中のため削除できませんでした: {0}。ゲームを終了してからもう一度お試しください。",
                "다음 파일이 사용 중이라 삭제하지 못했습니다: {0}. 게임을 종료한 뒤 다시 시도하세요.",
                "以下文件正在使用中，无法删除：{0}。请关闭游戏后重试。",
                "Эти файлы заняты и не были удалены: {0}. Закройте игру и повторите попытку." } },
            { "music.err.verify", new[]{
                "{0} did not copy correctly.", "{0} wurde nicht korrekt kopiert.", "{0} no se copió correctamente.", "{0} não foi copiado corretamente.", "{0} non è stato copiato correttamente.", "{0} n'a pas été copié correctement.", "{0} を正しくコピーできませんでした。", "{0} 을(를) 올바르게 복사하지 못했습니다.", "{0} 未能正确复制。", "{0} скопирован некорректно." } },
            { "music.log.copied", new[]{
                "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}", "  {0}  ←  {1}" } },
            { "music.dlg.title", new[]{
                "Check the order", "Reihenfolge prüfen", "Comprueba el orden", "Confira a ordem", "Controlla l'ordine", "Vérifiez l'ordre", "順番の確認", "순서 확인", "确认顺序", "Проверьте порядок" } },
            { "music.dlg.hint", new[]{
                "Drag a row to move a track to a different slot. The game picks tracks by number, so the order matters — play a track if you are unsure which one it is.",
                "Zieh eine Zeile, um einen Titel auf einen anderen Platz zu schieben. Das Spiel wählt Titel über ihre Nummer, die Reihenfolge zählt also — hör einen Titel an, wenn du unsicher bist.",
                "Arrastra una fila para mover una pista a otra posición. El juego elige las pistas por número, así que el orden importa: reproduce una pista si no estás seguro.",
                "Arraste uma linha para mover uma faixa para outra posição. O jogo escolhe faixas pelo número, então a ordem importa — toque uma faixa se estiver em dúvida.",
                "Trascina una riga per spostare un brano in un'altra posizione. Il gioco sceglie i brani per numero, quindi l'ordine conta: ascolta un brano se hai dubbi.",
                "Faites glisser une ligne pour déplacer un morceau. Le jeu choisit les morceaux par numéro : l'ordre compte — écoutez un morceau en cas de doute.",
                "行をドラッグすると曲を別の枠に移せます。ゲームは曲を番号で選ぶため順番が重要です。判断に迷ったら再生して確認してください。",
                "행을 끌어서 트랙을 다른 자리로 옮길 수 있습니다. 게임은 번호로 트랙을 고르므로 순서가 중요합니다 — 헷갈리면 재생해 확인하세요.",
                "拖动行可将曲目移到其他位置。游戏按编号选曲，因此顺序很重要 — 不确定时可以试听。",
                "Перетащите строку, чтобы переместить трек в другой слот. Игра выбирает треки по номеру, поэтому порядок важен — послушайте трек, если сомневаетесь." } },
            { "music.dlg.slot", new[]{
                "Slot", "Platz", "Posición", "Posição", "Posizione", "Emplacement", "枠", "자리", "位置", "Слот" } },
            { "music.dlg.file", new[]{
                "File", "Datei", "Archivo", "Arquivo", "File", "Fichier", "ファイル", "파일", "文件", "Файл" } },
            { "music.dlg.expected", new[]{
                "Expected", "Erwartet", "Esperado", "Esperado", "Atteso", "Attendu", "想定", "예상", "预期", "Ожидается" } },
            { "music.dlg.actual", new[]{
                "Actual", "Tatsächlich", "Real", "Real", "Effettivo", "Réel", "実際", "실제", "实际", "Фактически" } },
            { "music.dlg.match", new[]{
                "Match", "Treffer", "Coincidencia", "Correspondência", "Corrispondenza", "Correspondance", "一致", "일치", "匹配", "Совпадение" } },
            { "music.match.exact", new[]{
                "known file", "bekannte Datei", "archivo conocido", "arquivo conhecido", "file noto", "fichier connu", "既知のファイル", "알려진 파일", "已知文件", "известный файл" } },
            { "music.match.duration", new[]{
                "by length", "über die Länge", "por duración", "pela duração", "per durata", "par la durée", "長さで判定", "길이로 판정", "按时长", "по длительности" } },
            { "music.match.uncertain", new[]{
                "unsure", "unsicher", "dudoso", "incerto", "incerto", "incertain", "不確実", "불확실", "不确定", "не уверен" } },
            { "grp.globalcheats", new[]{
                "Always all players", "Immer alle Spieler", "Siempre todos los jugadores", "Sempre todos os jogadores", "Sempre tutti i giocatori", "Toujours tous les joueurs", "常に全プレイヤー", "항상 모든 플레이어", "始终对所有玩家", "Всегда для всех игроков" } },
            { "grp.unlock", new[]{
                "Unlock parts and superweapons of other factions", "Teile und Superwaffen anderer Fraktionen freischalten", "Desbloquear piezas y superarmas de otras facciones", "Desbloquear peças e superarmas de outras facções", "Sblocca parti e superarmi di altre fazioni", "Débloquer les pièces et superarmes d'autres factions", "他勢力のパーツとスーパーウェポンを解禁", "다른 진영의 부품과 슈퍼무기 잠금 해제", "解锁其他阵营的部件与超级武器", "Разблокировать детали и супероружие других фракций" } },
            { "scope.me", new[]{
                "Me only", "Nur ich", "Solo yo", "Só eu", "Solo io", "Moi seulement", "自分のみ", "나만", "仅自己", "Только я" } },
            { "scope.all", new[]{
                "All players (incl. AI)", "Alle Spieler (inkl. KI)", "Todos los jugadores (incl. IA)", "Todos os jogadores (incl. IA)", "Tutti i giocatori (incl. IA)", "Tous les joueurs (IA incl.)", "全プレイヤー（AI含む）", "모든 플레이어(AI 포함)", "所有玩家（含 AI）", "Все игроки (вкл. ИИ)" } },
            { "scope.note", new[]{
                "The scope above applies to Free building and Instant build. (No fog only ever affects your own view.)", "Die Auswahl oben gilt für Freies Bauen und Sofortbau. (Kein Nebel betrifft immer nur deine eigene Sicht.)", "El ámbito de arriba se aplica a Construcción gratis y Construcción instantánea. (Sin niebla solo afecta a tu propia vista.)", "O escopo acima se aplica a Construção grátis e Construção instantânea. (Sem névoa só afeta a sua própria visão.)", "L'ambito sopra vale per Costruzione gratuita e Costruzione istantanea. (Nessuna nebbia riguarda solo la tua visuale.)", "La portée ci-dessus s'applique à Construction gratuite et Construction instantanée. (Sans brouillard n'affecte que votre propre vue.)", "上記の範囲は「無料建設」と「即時建設」に適用されます。（霧なしは自分の視界のみに影響します。）", "위 범위는 무료 건설과 즉시 건설에 적용됩니다. (안개 제거는 항상 자신의 시야에만 영향을 줍니다.)", "上面的范围适用于免费建造和即时建造。（无战争迷雾仅影响你自己的视野。）", "Область выше относится к бесплатному и мгновенному строительству. (Без тумана влияет только на ваш обзор.)" } },
            { "cheat.fog", new[]{
                "No fog of war", "Kein Nebel des Krieges", "Sin niebla de guerra", "Sem névoa de guerra", "Nessuna nebbia di guerra", "Sans brouillard de guerre", "戦場の霧なし", "전장의 안개 제거", "无战争迷雾", "Без тумана войны" } },
            { "cheat.build", new[]{
                "Free building", "Freies Bauen", "Construcción gratis", "Construção grátis", "Costruzione gratuita", "Construction gratuite", "無料建設", "무료 건설", "免费建造", "Бесплатное строительство" } },
            { "cheat.turbo", new[]{
                "Instant build", "Sofortbau", "Construcción instantánea", "Construção instantânea", "Costruzione istantanea", "Construction instantanée", "即時建設", "즉시 건설", "即时建造", "Мгновенное строительство" } },
            { "cheat.crews", new[]{
                "Unlimited high-tier crews", "Unbegrenzte Elite-Crews", "Tripulaciones de alto nivel ilimitadas", "Tripulações de alto nível ilimitadas", "Equipaggi di alto livello illimitati", "Équipages de haut niveau illimités", "高ティア搭乗員が無制限", "고티어 대원 무제한", "无限高阶机组", "Неограниченные экипажи высокого ранга" } },
            { "cheat.crews.note", new[]{
                "Also lifts the ~50 combot limit, even without the Maximum version.",
                "Hebt auch das ~50-Combot-Limit auf, selbst ohne die Maximum-Version.",
                "También elimina el límite de ~50 combots, incluso sin la versión Máxima.",
                "Também remove o limite de ~50 combots, mesmo sem a versão Máxima.",
                "Rimuove anche il limite di ~50 combot, anche senza la versione Massima.",
                "Supprime aussi la limite de ~50 combots, même sans la version Maximum.",
                "「最大」バージョンでなくても、約50体のコンボット制限も解除します。",
                "'최대' 버전이 아니어도 약 50 콤봇 제한도 해제합니다.",
                "即使未选择“最大”版本，也会解除约 50 台 Combot 的上限。",
                "Также снимает лимит ~50 комботов, даже без версии «Максимум»." } },
            { "unlock.for", new[]{
                "For:", "Für:", "Para:", "Para:", "Per:", "Pour :", "対象：", "대상:", "对象：", "Для:" } },
            { "unlock.note", new[]{
                "Only parts of other factions are unlocked — your own faction's parts that require a building (e.g. a research center or AI facility) still need that building.\nAlien parts built during the prebuild phase must be re-researched at a research center afterwards.", "Nur Teile anderer Fraktionen werden freigeschaltet — Teile deiner eigenen Fraktion, die ein Gebäude voraussetzen (z. B. Forschungszentrum oder K.I.-Anlage), brauchen weiterhin dieses Gebäude.\nAlien-Teile, die in der Vorbereitungsphase gebaut werden, müssen danach im Forschungszentrum erneut erforscht werden.", "Solo se desbloquean piezas de otras facciones; las piezas de tu facción que requieren un edificio (p. ej. un centro de investigación o una instalación de IA) siguen necesitando ese edificio.\nLas piezas alienígenas construidas en la fase de preparación deben reinvestigarse después en un centro de investigación.", "Só se desbloqueiam peças de outras facções; as peças da sua facção que exigem um edifício (p. ex. um centro de pesquisa ou uma instalação de IA) ainda precisam desse edifício.\nPeças alienígenas construídas na fase de preparação precisam ser repesquisadas depois num centro de pesquisa.", "Vengono sbloccate solo parti di altre fazioni; le parti della tua fazione che richiedono un edificio (es. un centro di ricerca o un impianto IA) necessitano ancora di quell'edificio.\nLe parti aliene costruite nella fase di preparazione vanno riricercate dopo in un centro di ricerca.", "Seules les pièces d'autres factions sont débloquées ; les pièces de votre faction qui nécessitent un bâtiment (p. ex. un centre de recherche ou une installation IA) requièrent toujours ce bâtiment.\nLes pièces alien construites en phase de préparation doivent être recherchées à nouveau ensuite dans un centre de recherche.", "解禁されるのは他勢力のパーツのみです。自勢力のパーツで建物（研究施設やAI施設など）が必要なものは、引き続きその建物が必要です。\n準備フェイズ中に製造したエイリアンパーツは、その後に研究施設で再研究が必要です。", "다른 진영의 부품만 잠금 해제됩니다 — 자기 진영에서 건물(연구소, AI 시설 등)이 필요한 부품은 여전히 그 건물이 있어야 합니다.\n준비 단계에서 제작한 에일리언 부품은 이후 연구소에서 다시 연구해야 합니다.", "仅解锁其他阵营的部件——你自己阵营中需要特定建筑（如研究中心或 AI 设施）的部件仍需建造该建筑。\n在预备阶段建造的外星部件之后必须在研究中心重新研究。", "Разблокируются только детали других фракций — детали вашей фракции, требующие здания (например, исследовательского центра или ИИ-комплекса), по-прежнему нуждаются в этом здании.\nИнопланетные детали, построенные на этапе подготовки, затем нужно заново исследовать в исследовательском центре." } },
            { "tree.superweapons", new[]{
                "Superweapons", "Superwaffen", "Superarmas", "Superarmas", "Superarmi", "Superarmes", "スーパーウェポン", "슈퍼무기", "超级武器", "Супероружие" } },
            { "note.fogsv", new[]{
                "— Shared vision is on (Patch tab)", "— Geteilte Sicht ist an (Patch-Tab)", "— Visión compartida activada (pestaña Parche)", "— Visão compartilhada ativada (aba Patch)", "— Visione condivisa attiva (scheda Patch)", "— Vision partagée activée (onglet Patch)", "— 視界共有がオン（パッチタブ）", "— 시야 공유 켜짐 (패치 탭)", "— 已开启共享视野（补丁标签）", "— Общий обзор включён (вкладка «Патч»)" } },
            { "note.svfog", new[]{
                "Disabled — \"No fog of war\" (Cheats tab) already reveals the whole map.", "Deaktiviert — \"Kein Nebel des Krieges\" (Cheats-Tab) deckt bereits die ganze Karte auf.", "Desactivado — \"Sin niebla de guerra\" (pestaña Trucos) ya revela todo el mapa.", "Desativado — \"Sem névoa de guerra\" (aba Cheats) já revela todo o mapa.", "Disattivato — \"Nessuna nebbia di guerra\" (scheda Trucchi) rivela già tutta la mappa.", "Désactivé — \"Sans brouillard de guerre\" (onglet Triche) révèle déjà toute la carte.", "無効 — 「戦場の霧なし」（チートタブ）で既に全マップが表示されます。", "비활성화됨 — \"전장의 안개 제거\"(치트 탭)가 이미 전체 지도를 드러냅니다.", "已禁用 —「无战争迷雾」（秘籍标签）已显示整张地图。", "Отключено — «Без тумана войны» (вкладка «Читы») уже открывает всю карту." } },
            { "slot.arm", new[]{
                "Arm", "Arm", "Brazo", "Braço", "Braccio", "Bras", "アーム", "팔", "手臂", "Рука" } },
            { "slot.legs", new[]{
                "Legs", "Beine", "Piernas", "Pernas", "Gambe", "Jambes", "レッグ", "다리", "腿部", "Ноги" } },
            { "slot.torso", new[]{
                "Torso", "Torso", "Torso", "Torso", "Torso", "Torse", "トルソー", "몸통", "躯干", "Торс" } },
        };
    }
}
