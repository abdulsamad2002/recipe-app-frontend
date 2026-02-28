import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  username = '';
  email    = '';
  password = '';
  error    = signal<string | null>(null);
  loading  = signal(false);

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    if (!this.username || !this.email || !this.password) {
      this.error.set('Please fill in all fields.');
      return;
    }
    if (this.password.length < 6) {
      this.error.set('Password must be at least 6 characters.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.auth.register({ username: this.username, email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Registration failed. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
