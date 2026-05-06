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
