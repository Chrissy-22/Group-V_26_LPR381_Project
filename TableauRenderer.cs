using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Group_V_26_LPR381_Project.Models;

namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    public static class TableauRenderer
    {
        // ============================================================
        // OPTIMA THEME COLOURS
        // ============================================================

        private static readonly Color BackgroundColor =
            Color.FromArgb(248, 250, 252);

        private static readonly Color PrimaryTextColor =
            Color.FromArgb(15, 23, 42);

        private static readonly Color SecondaryTextColor =
            Color.FromArgb(71, 85, 105);

        private static readonly Color AccentColor =
            Color.FromArgb(37, 99, 235);

        private static readonly Color HeaderBackgroundColor =
            Color.FromArgb(226, 232, 240);

        private static readonly Color PivotBackgroundColor =
            Color.FromArgb(219, 234, 254);

        private static readonly Color PivotTextColor =
            Color.FromArgb(30, 64, 175);

        private static readonly Color SuccessColor =
            Color.FromArgb(22, 163, 74);

        private static readonly Color ErrorColor =
            Color.FromArgb(220, 38, 38);

        private static readonly Color RuleColor =
            Color.FromArgb(203, 213, 225);


        // ============================================================
        // FONTS
        // ============================================================

        private static readonly Font BodyFont =
            new Font(
                "Consolas",
                10F,
                FontStyle.Regular
            );

        private static readonly Font BodyBoldFont =
            new Font(
                "Consolas",
                10F,
                FontStyle.Bold
            );

        private static readonly Font UiBoldFont =
            new Font(
                "Segoe UI",
                10F,
                FontStyle.Bold
            );

        private static readonly Font SectionHeaderFont =
            new Font(
                "Segoe UI",
                12F,
                FontStyle.Bold
            );

        private static readonly Font GroupHeaderFont =
            new Font(
                "Segoe UI",
                13F,
                FontStyle.Bold
            );

        private static readonly Font SmallFont =
            new Font(
                "Segoe UI",
                9F,
                FontStyle.Regular
            );


        // ============================================================
        // TABLE CONFIGURATION
        // ============================================================

        private const int CellPadding =
            26;

        private const int MaxTabStops =
            32;


        // ============================================================
        // PLAIN LINE
        // ============================================================

        public static void AppendPlainLine(
            RichTextBox rtb,
            string text)
        {
            if (rtb == null)
                return;


            AppendText(
                rtb,
                (text ?? "") + Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );
        }


        // ============================================================
        // TEXT BLOCK
        // ============================================================

        public static void AppendTextBlock(
            RichTextBox rtb,
            string title,
            string text)
        {
            if (rtb == null)
                return;


            // --------------------------------------------------------
            // TITLE
            // --------------------------------------------------------

            if (
                !string.IsNullOrWhiteSpace(
                    title))
            {
                AppendText(
                    rtb,
                    title + Environment.NewLine,
                    PrimaryTextColor,
                    BackgroundColor,
                    SectionHeaderFont
                );
            }


            // --------------------------------------------------------
            // BODY
            // --------------------------------------------------------

            if (
                !string.IsNullOrWhiteSpace(
                    text))
            {
                AppendText(
                    rtb,
                    text + Environment.NewLine,
                    SecondaryTextColor,
                    BackgroundColor,
                    BodyFont
                );
            }


            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );
        }


        // ============================================================
        // GROUP HEADER
        // ============================================================

        public static void AppendGroupHeader(
            RichTextBox rtb,
            string title,
            int indentLevel = 0)
        {
            if (rtb == null)
                return;


            string indent =
                new string(
                    ' ',
                    Math.Max(
                        0,
                        indentLevel
                    ) * 4
                );


            // --------------------------------------------------------
            // SPACING BEFORE HEADER
            // --------------------------------------------------------

            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );


            // --------------------------------------------------------
            // HEADER TITLE
            // --------------------------------------------------------

            AppendText(
                rtb,
                indent +
                (title ?? "") +
                Environment.NewLine,

                AccentColor,
                BackgroundColor,
                GroupHeaderFont
            );


            // --------------------------------------------------------
            // DIVIDER
            // --------------------------------------------------------

            AppendText(
                rtb,
                indent +
                new string('─', 55) +
                Environment.NewLine,

                RuleColor,
                BackgroundColor,
                SmallFont
            );


            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );
        }


        // ============================================================
        // TABLEAU
        // ============================================================

        public static void AppendTableau(
            RichTextBox rtb,
            double[,] tableau,
            List<string> columnHeaders,
            int pivotRow,
            int pivotCol,
            string title = null)
        {
            if (
                rtb == null ||
                tableau == null)
            {
                return;
            }


            int rows =
                tableau.GetLength(0);

            int cols =
                tableau.GetLength(1);


            if (
                rows <= 0 ||
                cols <= 0)
            {
                return;
            }


            // ========================================================
            // COLUMN HEADERS
            // ========================================================

            string[] headers;


            if (
                columnHeaders != null &&
                columnHeaders.Count == cols)
            {
                headers =
                    columnHeaders.ToArray();
            }

            else
            {
                headers =
                    Enumerable
                    .Range(0, cols)
                    .Select(
                        index => $"C{index}"
                    )
                    .ToArray();
            }


            // ========================================================
            // FORMAT TABLE VALUES
            // ========================================================

            string[,] cellText =
                new string[rows, cols];


            for (
                int row = 0;
                row < rows;
                row++)
            {
                for (
                    int column = 0;
                    column < cols;
                    column++)
                {
                    double value =
                        tableau[
                            row,
                            column
                        ];


                    if (double.IsNaN(value))
                    {
                        cellText[
                            row,
                            column
                        ] = "-";
                    }

                    else if (
                        double.IsPositiveInfinity(
                            value))
                    {
                        cellText[
                            row,
                            column
                        ] = "∞";
                    }

                    else if (
                        double.IsNegativeInfinity(
                            value))
                    {
                        cellText[
                            row,
                            column
                        ] = "-∞";
                    }

                    else
                    {
                        cellText[
                            row,
                            column
                        ] =
                            NumberFormatter.Format(
                                value
                            );
                    }
                }
            }


            // ========================================================
            // TABLE TITLE
            // ========================================================

            if (
                !string.IsNullOrWhiteSpace(
                    title))
            {
                AppendText(
                    rtb,
                    title +
                    Environment.NewLine,

                    PrimaryTextColor,
                    BackgroundColor,
                    SectionHeaderFont
                );


                AppendText(
                    rtb,
                    new string('─', 55) +
                    Environment.NewLine,

                    RuleColor,
                    BackgroundColor,
                    SmallFont
                );


                AppendText(
                    rtb,
                    Environment.NewLine,
                    PrimaryTextColor,
                    BackgroundColor,
                    BodyFont
                );
            }


            // ========================================================
            // CALCULATE COLUMN WIDTHS
            // ========================================================

            int labelWidthPx =
                CalculateRowLabelWidth(
                    rows
                );


            int[] columnWidthsPx =
                CalculateColumnWidths(
                    headers,
                    cellText,
                    rows,
                    cols
                );


            int[] tabStops =
                BuildTabStops(
                    labelWidthPx,
                    columnWidthsPx
                );


            int blockStart =
                rtb.TextLength;


            // ========================================================
            // HEADER ROW
            // ========================================================

            AppendText(
                rtb,
                " ",
                PrimaryTextColor,
                HeaderBackgroundColor,
                UiBoldFont
            );


            AppendText(
                rtb,
                "\t",
                PrimaryTextColor,
                HeaderBackgroundColor,
                BodyFont
            );


            for (
                int column = 0;
                column < cols;
                column++)
            {
                bool isPivotColumn =
                    column == pivotCol;


                Color background =
                    isPivotColumn
                        ? PivotBackgroundColor
                        : HeaderBackgroundColor;


                Color foreground =
                    isPivotColumn
                        ? PivotTextColor
                        : PrimaryTextColor;


                AppendText(
                    rtb,
                    headers[column],
                    foreground,
                    background,
                    UiBoldFont
                );


                AppendText(
                    rtb,
                    "\t",
                    foreground,
                    background,
                    BodyFont
                );
            }


            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );


            // ========================================================
            // DATA ROWS
            // ========================================================

            for (
                int row = 0;
                row < rows;
                row++)
            {
                bool isPivotRow =
                    row == pivotRow;


                Color rowLabelBackground =
                    isPivotRow
                        ? PivotBackgroundColor
                        : BackgroundColor;


                Color rowLabelForeground =
                    isPivotRow
                        ? PivotTextColor
                        : PrimaryTextColor;


                // ----------------------------------------------------
                // ROW LABEL
                // ----------------------------------------------------

                AppendText(
                    rtb,
                    GetRowLabel(row),
                    rowLabelForeground,
                    rowLabelBackground,
                    BodyBoldFont
                );


                AppendText(
                    rtb,
                    "\t",
                    rowLabelForeground,
                    rowLabelBackground,
                    BodyFont
                );


                // ----------------------------------------------------
                // CELLS
                // ----------------------------------------------------

                for (
                    int column = 0;
                    column < cols;
                    column++)
                {
                    bool isPivotColumn =
                        column == pivotCol;


                    bool isPivotCell =
                        isPivotRow &&
                        isPivotColumn;


                    bool highlight =
                        isPivotRow ||
                        isPivotColumn;


                    Color background =
                        highlight
                            ? PivotBackgroundColor
                            : BackgroundColor;


                    Color foreground =
                        highlight
                            ? PivotTextColor
                            : PrimaryTextColor;


                    Font font =
                        isPivotCell
                            ? BodyBoldFont
                            : BodyFont;


                    AppendText(
                        rtb,
                        cellText[
                            row,
                            column
                        ],
                        foreground,
                        background,
                        font
                    );


                    AppendText(
                        rtb,
                        "\t",
                        foreground,
                        background,
                        BodyFont
                    );
                }


                AppendText(
                    rtb,
                    Environment.NewLine,
                    PrimaryTextColor,
                    BackgroundColor,
                    BodyFont
                );
            }


            // ========================================================
            // APPLY TAB STOPS
            // ========================================================

            int selectionLength =
                rtb.TextLength -
                blockStart;


            if (selectionLength > 0)
            {
                rtb.Select(
                    blockStart,
                    selectionLength
                );


                rtb.SelectionTabs =
                    tabStops;


                rtb.Select(
                    rtb.TextLength,
                    0
                );
            }


            // ========================================================
            // SPACING AFTER TABLE
            // ========================================================

            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );
        }


        // ============================================================
        // SUCCESS MESSAGE
        // ============================================================

        public static void AppendSuccess(
            RichTextBox rtb,
            string text)
        {
            if (rtb == null)
                return;


            AppendText(
                rtb,
                "✓ " +
                (text ?? "") +
                Environment.NewLine,

                SuccessColor,
                BackgroundColor,
                UiBoldFont
            );
        }


        // ============================================================
        // ERROR MESSAGE
        // ============================================================

        public static void AppendError(
            RichTextBox rtb,
            string text)
        {
            if (rtb == null)
                return;


            AppendText(
                rtb,
                "✕ " +
                (text ?? "") +
                Environment.NewLine,

                ErrorColor,
                BackgroundColor,
                UiBoldFont
            );
        }


        // ============================================================
        // SECTION LABEL
        // ============================================================

        public static void AppendSectionLabel(
            RichTextBox rtb,
            string text)
        {
            if (rtb == null)
                return;


            AppendText(
                rtb,
                (text ?? "") +
                Environment.NewLine,

                SecondaryTextColor,
                BackgroundColor,
                UiBoldFont
            );


            AppendText(
                rtb,
                Environment.NewLine,
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );
        }


        // ============================================================
        // LABEL + RESULT VALUE
        //
        // Example:
        //
        // Objective Value        21
        // x1                     3
        // x2                     1.5
        //
        // This is available if we later want to use it in
        // RenderSolution().
        // ============================================================

        public static void AppendResultValue(
            RichTextBox rtb,
            string label,
            string value)
        {
            if (rtb == null)
                return;


            int startPosition =
                rtb.TextLength;


            AppendText(
                rtb,
                label ?? "",
                PrimaryTextColor,
                BackgroundColor,
                BodyFont
            );


            AppendText(
                rtb,
                "\t" +
                (value ?? "") +
                Environment.NewLine,

                AccentColor,
                BackgroundColor,
                BodyBoldFont
            );


            int length =
                rtb.TextLength -
                startPosition;


            if (length > 0)
            {
                rtb.Select(
                    startPosition,
                    length
                );


                rtb.SelectionTabs =
                    new int[]
                    {
                        220
                    };


                rtb.Select(
                    rtb.TextLength,
                    0
                );
            }
        }


        // ============================================================
        // INTERNAL HELPERS
        // ============================================================

        private static string GetRowLabel(
            int row)
        {
            // Row zero is the objective function.
            if (row == 0)
                return "Z";


            return $"R{row}";
        }


        // ------------------------------------------------------------
        // ROW LABEL WIDTH
        // ------------------------------------------------------------

        private static int CalculateRowLabelWidth(
            int rows)
        {
            int maxWidth =
                0;


            for (
                int row = 0;
                row < rows;
                row++)
            {
                int width =
                    TextRenderer.MeasureText(
                        GetRowLabel(row),
                        BodyBoldFont
                    ).Width;


                maxWidth =
                    Math.Max(
                        maxWidth,
                        width
                    );
            }


            return
                maxWidth +
                CellPadding;
        }


        // ------------------------------------------------------------
        // COLUMN WIDTHS
        // ------------------------------------------------------------

        private static int[] CalculateColumnWidths(
            string[] headers,
            string[,] cellText,
            int rows,
            int cols)
        {
            int[] widths =
                new int[cols];


            for (
                int column = 0;
                column < cols;
                column++)
            {
                int maxWidth =
                    TextRenderer.MeasureText(
                        headers[column],
                        UiBoldFont
                    ).Width;


                for (
                    int row = 0;
                    row < rows;
                    row++)
                {
                    int valueWidth =
                        TextRenderer.MeasureText(
                            cellText[
                                row,
                                column
                            ],
                            BodyFont
                        ).Width;


                    if (valueWidth > maxWidth)
                    {
                        maxWidth =
                            valueWidth;
                    }
                }


                widths[column] =
                    maxWidth +
                    CellPadding;
            }


            return widths;
        }


        // ------------------------------------------------------------
        // TAB STOPS
        // ------------------------------------------------------------

        private static int[] BuildTabStops(
            int labelWidth,
            int[] columnWidths)
        {
            List<int> stops =
                new List<int>();


            int cumulativeWidth =
                labelWidth;


            stops.Add(
                cumulativeWidth
            );


            foreach (
                int width
                in columnWidths)
            {
                cumulativeWidth +=
                    width;


                stops.Add(
                    cumulativeWidth
                );


                if (
                    stops.Count >=
                    MaxTabStops)
                {
                    break;
                }
            }


            return
                stops.ToArray();
        }


        // ------------------------------------------------------------
        // LOW LEVEL TEXT WRITER
        // ------------------------------------------------------------

        private static void AppendText(
            RichTextBox rtb,
            string text,
            Color foreColor,
            Color backColor,
            Font font)
        {
            if (rtb == null)
                return;


            rtb.SelectionStart =
                rtb.TextLength;

            rtb.SelectionLength =
                0;


            rtb.SelectionFont =
                font;

            rtb.SelectionColor =
                foreColor;

            rtb.SelectionBackColor =
                backColor;


            rtb.AppendText(
                text ?? ""
            );


            // --------------------------------------------------------
            // RESET FORMATTING
            //
            // Otherwise the next text added to the RichTextBox can
            // accidentally inherit the previous colour/background.
            // --------------------------------------------------------

            rtb.SelectionStart =
                rtb.TextLength;

            rtb.SelectionLength =
                0;

            rtb.SelectionFont =
                BodyFont;

            rtb.SelectionColor =
                PrimaryTextColor;

            rtb.SelectionBackColor =
                BackgroundColor;
        }
    }
}