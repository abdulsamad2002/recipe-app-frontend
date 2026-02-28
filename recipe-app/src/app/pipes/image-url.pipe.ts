import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../environments/environment';

/**
 * Converts a server-relative image path (e.g. /uploads/recipes/abc.jpg)
 * into an absolute URL pointing at the .NET backend media server.
 *
 * Usage in template:
 *   <img [src]="recipe.imageUrl | imageUrl" />
 *
 * Returns null (no src) when imageUrl is null/undefined/empty.
 */
@Pipe({
  name: 'imageUrl',
  standalone: true,
  pure: true
})
export class ImageUrlPipe implements PipeTransform {
  transform(value: string | null | undefined): string | null {
    if (!value) return null;

    // Already absolute (http:// or https://) — leave it unchanged
    if (value.startsWith('http://') || value.startsWith('https://')) {
      return value;
    }

    // Relative path — prepend the backend media origin
    const base = environment.mediaBase.replace(/\/$/, '');
    const path = value.startsWith('/') ? value : `/${value}`;
    return `${base}${path}`;
  }
}
