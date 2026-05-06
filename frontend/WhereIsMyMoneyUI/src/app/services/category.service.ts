import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Category } from '../models/category/Category';
import { PaginatedResponse } from '../models/api/paginated-response.model';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly categories = signal<Category[]>([]);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/categories/';
  private readonly defaultPageSize = 15;

  async getCategories(
    pageNumber: number = 1,
    pageSize: number = this.defaultPageSize,
  ): Promise<PaginatedResponse<Category> | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.get<PaginatedResponse<Category>>(this.baseApiUrl, {
        params: {
          pageNumber,
          pageSize,
        },
      });

      this.categories.set(response.items);
      return response;
    } catch (error) {
      this.error.set('Failed to load categories.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async updateCategory(
    id: number,
    category: Partial<Pick<Category, 'name' | 'budget'>>,
  ): Promise<Category | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const updated = await this.api.patch<Category>(`${this.baseApiUrl}${id}`, category);
      this.categories.update((current) => current.map((c) => (c.id === id ? updated : c)));
      return updated;
    } catch {
      this.error.set('Failed to update category.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteCategory(id: number): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.delete<Category>(`${this.baseApiUrl}${id}`);
      this.categories.update((current) => current.filter((c) => c.id !== id));
    } catch {
      this.error.set('Failed to delete category.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async createCategory(category: Omit<Category, 'id'>): Promise<Category | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const createdCategory = await this.api.post<Category>(this.baseApiUrl, category);
      this.categories.update((current) => [...current, createdCategory]);
      return createdCategory;
    } catch (error) {
      this.error.set('Failed to create category.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  clearCategories(): void {
    this.categories.set([]);
  }
}
