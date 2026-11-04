import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react-native';
import SearchBar from '@/components/searchBar';

describe('SearchBar', () => {
  it('submits the entered search query', async () => {
    const onSubmitSearch = jest.fn();
    await render(
      <SearchBar onSubmitSearch={onSubmitSearch} />,
    );
    const input = screen.getByPlaceholderText('Search...');

    await fireEvent.changeText(input, 'Dune');
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).toHaveBeenCalledWith('Dune');
  });

  it('does not submit an empty query', async () => {
    const onSubmitSearch = jest.fn();
    await render(
      <SearchBar onSubmitSearch={onSubmitSearch} />,
    );
    const input = screen.getByPlaceholderText('Search...');

    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).not.toHaveBeenCalled();
  });

  it('does not submit a whitespace-only query', async () => {
    const onSubmitSearch = jest.fn();
    await render(<SearchBar onSubmitSearch={onSubmitSearch} />);
    const input = screen.getByPlaceholderText('Search...');

    await fireEvent.changeText(input, '   ');
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).not.toHaveBeenCalled();
  });

  it('trims surrounding whitespace before submitting', async () => {
    const onSubmitSearch = jest.fn();
    await render(<SearchBar onSubmitSearch={onSubmitSearch} />);
    const input = screen.getByPlaceholderText('Search...');

    await fireEvent.changeText(input, '  Dune  ');
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).toHaveBeenCalledWith('Dune');
  });

  it('does not submit when the input value is null at runtime', async () => {
    const onSubmitSearch = jest.fn();
    await render(<SearchBar onSubmitSearch={onSubmitSearch} />);
    const input = screen.getByPlaceholderText('Search...');

    await fireEvent.changeText(input, null as unknown as string);
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).not.toHaveBeenCalled();
  });
});
