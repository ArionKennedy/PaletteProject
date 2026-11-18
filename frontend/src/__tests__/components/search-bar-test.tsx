import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react-native';
import SearchBar from '@/components/searchBar';

describe('<SearchBar />', () => {
  test('renders the search input', () => {
    render(<SearchBar onSubmitSearch={jest.fn()} />);

    expect(screen.getByPlaceholderText('Search...')).toBeTruthy();
  });

  test('calls onSubmitSearch with the query when submitted', () => {
    const onSubmitSearch = jest.fn();
    render(<SearchBar onSubmitSearch={onSubmitSearch} />);

    const input = screen.getByPlaceholderText('Search...');
    fireEvent.changeText(input, 'batman');
    fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).toHaveBeenCalledWith('batman');
    expect(onSubmitSearch).toHaveBeenCalledTimes(1);
  });

  test('does not call onSubmitSearch when the query is empty', () => {
    const onSubmitSearch = jest.fn();
    render(<SearchBar onSubmitSearch={onSubmitSearch} />);

    const input = screen.getByPlaceholderText('Search...');
    fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).not.toHaveBeenCalled();
  });
});
