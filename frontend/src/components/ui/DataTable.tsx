'use client';

import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react';
import { Button } from './Button';

export interface Column<T> {
  header: string;
  accessorKey?: keyof T;
  cell?: (item: T) => React.ReactNode;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  isLoading?: boolean;
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  emptyMessage?: string;
  rowClassName?: (item: T) => string;
}

export function DataTable<T>({ 
  data, 
  columns, 
  isLoading, 
  page, 
  totalPages, 
  onPageChange,
  emptyMessage = "No results found.",
  rowClassName
}: DataTableProps<T>) {
  
  return (
    <div className="bg-background border border-border rounded-lg overflow-hidden flex flex-col shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full text-sm text-left">
          <thead className="text-xs text-muted-foreground uppercase bg-muted/50 border-b border-border">
            <tr>
              {columns.map((col, idx) => (
                <th key={idx} className="px-6 py-4 font-medium tracking-wider">
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {isLoading ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center">
                  <Loader2 className="w-6 h-6 animate-spin mx-auto text-muted-foreground" />
                  <p className="mt-2 text-muted-foreground">Loading data...</p>
                </td>
              </tr>
            ) : data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center text-muted-foreground">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              data.map((item, rowIdx) => (
                <tr key={rowIdx} className={`hover:bg-muted/30 transition-colors ${rowClassName ? rowClassName(item) : ''}`}>
                  {columns.map((col, colIdx) => (
                    <td key={colIdx} className="px-6 py-4 whitespace-nowrap">
                      {col.cell ? col.cell(item) : String(item[col.accessorKey as keyof T] || '')}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      
      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between px-6 py-3 border-t border-border bg-muted/20">
          <span className="text-sm text-muted-foreground">
            Page <span className="font-medium text-foreground">{page}</span> of <span className="font-medium text-foreground">{totalPages}</span>
          </span>
          <div className="flex space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1 || isLoading}
            >
              <ChevronLeft className="w-4 h-4 mr-1" />
              Prev
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages || isLoading}
            >
              Next
              <ChevronRight className="w-4 h-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
