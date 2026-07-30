import { Injectable } from '@angular/core';

export interface ExcelColumn<T> {
  header: string;
  value: (row: T) => string | number | boolean | null | undefined;
}

@Injectable({ providedIn: 'root' })
export class ExcelExportService {
  /**
   * Builds a UTF-8 CSV (Excel-friendly) and triggers a browser download.
   */
  exportCsv<T>(filename: string, rows: T[], columns: ExcelColumn<T>[]): void {
    const header = columns.map((column) => this.escape(column.header)).join(',');
    const lines = rows.map((row) =>
      columns
        .map((column) => this.escape(this.stringify(column.value(row))))
        .join(',')
    );
    const csv = `\uFEFF${[header, ...lines].join('\r\n')}`;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    const safeName = filename.toLowerCase().endsWith('.csv') ? filename : `${filename}.csv`;
    link.href = url;
    link.download = safeName;
    link.click();
    URL.revokeObjectURL(url);
  }

  private stringify(value: string | number | boolean | null | undefined): string {
    if (value === null || value === undefined) return '';
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return String(value);
  }

  private escape(value: string): string {
    if (/[",\r\n]/.test(value)) {
      return `"${value.replace(/"/g, '""')}"`;
    }
    return value;
  }
}
